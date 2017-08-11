using namespace System.Collections.Generic
using namespace System.Collections.ObjectModel
using namespace System.Linq.Expressions
using namespace System.Management.Automation
using namespace System.Management.Automation.Runspaces
using namespace System.Threading.Tasks

# Static class that facilitates the use of traditional .NET async techniques in PowerShell.
class AsyncOps
{
    static [PSTaskFactory] $Factory = [PSTaskFactory]::new();
    static [RunspacePool] $RunspacePool;

    # Hides the result property on a Task object. This is done because the getter for Result waits
    # for the task to finish, even if just output to the console.
    static [Task] HideResult([Task] $target) {
        $propertyList    = $target.psobject.Properties.Name -notmatch 'Result' -as [string[]]
        $propertySet     = [PSPropertySet]::new('DefaultDisplayPropertySet', $propertyList) -as [PSMemberInfo[]]
        $standardMembers = [PSMemberSet]::new('PSStandardMembers', $propertySet)

        $target.psobject.Members.Add($standardMembers)

        return $target
    }

    # Create a delegate from a scriptblock that can be used in threads without runspaces, like those
    # used in Tasks or AsyncCallbacks.
    static [MulticastDelegate] CreateAsyncDelegate([scriptblock] $function, [type] $delegateType) {
        # Create a runspace pool the first time this method is invoked.
        if (-not [AsyncOps]::RunspacePool) {
            [AsyncOps]::RunspacePool = [runspacefactory]::CreateRunspacePool(1, 4)
            [AsyncOps]::RunspacePool.Open()
        }

        # Create a parameter expression for each parameter the delegate takes.
        $parameters = $delegateType.
            GetMethod('Invoke').
            GetParameters().
            ForEach{ [Expression]::Parameter($PSItem.ParameterType, $PSItem.Name) }

        # Set AsyncState variable that will hold delegate arguments and/or state.
        $preparedScript = 'param($AsyncState) . {{ {0} }}' -f $function

        # Prepare variable and constant expressions.
        $pool       = [Expression]::Property($null, [AsyncOps], 'RunspacePool')
        $scriptText = [Expression]::Constant($preparedScript, [string])
        $ps         = [Expression]::Variable([powershell], 'ps')
        $result     = [Expression]::Variable([Collection[psobject]], 'result')

        # Group the expressions for the body by creating them in a scriptblock.
        [Expression[]]$expressions = & {
            [Expression]::Assign($ps, [Expression]::Call([powershell], 'Create', @(), @()))
            [Expression]::Assign([Expression]::Property($ps, 'RunspacePool'), $pool)
            [Expression]::Call($ps, 'AddScript', @(), $scriptText)

            foreach ($parameter in $parameters) {
                [Expression]::Call($ps, 'AddArgument', @(), $parameter)
            }

            [Expression]::Assign($result, [Expression]::Call($ps, 'Invoke', @(), @()))
            [Expression]::Call($ps, 'Dispose', @(), @())
            $result
        }

        $block  = [Expression]::Block([ParameterExpression[]]($ps, $result), $expressions)
        $lambda = [Expression]::Lambda(
            $delegateType,
            $block,
            $parameters -as [ParameterExpression[]])
        return $lambda.Compile()
    }
}

# A TaskFactory implementation that creates tasks that run scriptblocks in a runspace pool.
class PSTaskFactory : TaskFactory[Collection[psobject]] {
    # Shortcut to AsyncOps.CreateAsyncDelegate
    hidden [MulticastDelegate] Wrap([scriptblock] $function, [type] $delegateType) {
        return [AsyncOps]::CreateAsyncDelegate($function, $delegateType)
    }

    # The remaining functions implement methods from TaskFactory. All of these methods call the base
    # method after wrapping the scriptblock to create a delegate that will work in tasks.
    [Task[Collection[psobject]]] ContinueWhenAll([Task[]] $tasks, [scriptblock] $continuationAction) {
        $delegateType = [Func`2].MakeGenericType([Task[]], [Collection[psobject]])
        return [AsyncOps]::HideResult(
            ([TaskFactory[Collection[psobject]]]$this).ContinueWhenAll(
                $tasks,
                $this.Wrap($continuationAction, $delegateType),
                $this.CancellationToken,
                $this.ContinuationOptions,
                [TaskScheduler]::Current))
    }

    [Task[Collection[psobject]]] ContinueWhenAny([Task[]] $tasks, [scriptblock] $continuationAction) {
        $delegateType = [Func`2].MakeGenericType([Task[]], [Collection[psobject]])
        return [AsyncOps]::HideResult(
            ([TaskFactory[Collection[psobject]]]$this).ContinueWhenAny(
                $tasks,
                $this.Wrap($continuationAction, $delegateType),
                $this.CancellationToken,
                $this.ContinuationOptions,
                [TaskScheduler]::Current))
    }

    [Task[Collection[psobject]]] StartNew([scriptblock] $function) {
        return [AsyncOps]::HideResult(
            ([TaskFactory[Collection[psobject]]]$this).StartNew(
                $this.Wrap($function, [Func[Collection[psobject]]]),
                $this.CancellationToken,
                $this.CreationOptions,
                [TaskScheduler]::Current))
    }

    [Task[Collection[psobject]]] StartNew([scriptblock] $function, [object] $state) {
        return [AsyncOps]::HideResult(
            ([TaskFactory[Collection[psobject]]]$this).StartNew(
                $this.Wrap($function, [Func[object, Collection[psobject]]]),
                $state,
                $this.CancellationToken,
                $this.CreationOptions,
                [TaskScheduler]::Current))
    }
}

function async {
    [CmdletBinding()]
    param(
        [scriptblock]
        $ScriptBlock,

        [Parameter(ValueFromPipeline)]
        [object]
        $ArgumentList
    )
    process {
        [AsyncOps]::Factory.StartNew($ScriptBlock, $ArgumentList)
    }
}

function await {
    [CmdletBinding()]
    param(
        [Parameter(ValueFromPipeline)]
        [Task]
        $Task
    )
    begin {
        $taskList = [List[Task]]::new()
    }
    process {
        $taskList.Add($Task)
    }
    end {
        return $taskList.Result
    }
}

function ContinueWith {
    [CmdletBinding()]
    param(
        [scriptblock]
        $ContinuationAction,

        [switch]
        $Any,

        [Parameter(ValueFromPipeline)]
        [Task]
        $Task
    )
    begin {
        $taskList = [List[Task]]::new()
    }
    process {
        $taskList.Add($Task)
    }
    end {
        if ($Any.IsPresent) {
            return [AsyncOps]::Factory.ContinueWhenAny($taskList, $ContinuationAction)
        }
        return [AsyncOps]::Factory.ContinueWhenAll($taskList, $ContinuationAction)
    }
}
