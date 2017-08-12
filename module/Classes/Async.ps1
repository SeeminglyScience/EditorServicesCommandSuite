using namespace System.Collections.Generic
using namespace System.Collections.ObjectModel
using namespace System.Linq.Expressions
using namespace System.Management.Automation
using namespace System.Management.Automation.Runspaces
using namespace System.Threading
using namespace System.Threading.Tasks

# Static class that facilitates the use of traditional .NET async techniques in PowerShell.
class AsyncOps {
    static [PSTaskFactory] $Factory = [PSTaskFactory]::new();

    static [Task] ContinueWithCodeMethod([psobject] $instance, [scriptblock] $continuationAction) {
        $delegate = [AsyncOps]::CreateAsyncDelegate(
            $continuationAction,
            [Action[Task[Collection[psobject]]]])

        return [AsyncOps]::PrepareTask($instance.psadapted.ContinueWith($delegate))
    }

    # - Hides the result property on a Task object. This is done because the getter for Result waits
    #   for the task to finish, even if just output to the console.
    # - Adds ContinueWith code method that wraps scriptblocks with CreateAsyncDelegate
    static [Task] PrepareTask([Task] $target) {
        $propertyList    = $target.psobject.Properties.Name -notmatch 'Result' -as [string[]]
        $propertySet     = [PSPropertySet]::new('DefaultDisplayPropertySet', $propertyList) -as [PSMemberInfo[]]
        $standardMembers = [PSMemberSet]::new('PSStandardMembers', $propertySet)

        $target.psobject.Members.Add($standardMembers)

        $target.psobject.Methods.Add(
            [PSCodeMethod]::new(
                'ContinueWith',
                [AsyncOps].GetMethod('ContinueWithCodeMethod')))

        return $target
    }

    static [MulticastDelegate] CreateAsyncDelegate([scriptblock] $function, [type] $delegateType) {
        return [AsyncOps]::CreateAsyncDelegate($function, $delegateType, [AsyncOps]::Factory)
    }

    # Create a delegate from a scriptblock that can be used in threads without runspaces, like those
    # used in Tasks or AsyncCallbacks.
    static [MulticastDelegate] CreateAsyncDelegate([scriptblock] $function, [type] $delegateType, [PSTaskFactory] $factory) {
        $invokeMethod = $delegateType.GetMethod('Invoke')
        $returnType = $invokeMethod.ReturnType
        # Create a parameter expression for each parameter the delegate takes.
        $parameters = $invokeMethod.
            GetParameters().
            ForEach{ [Expression]::Parameter($PSItem.ParameterType, $PSItem.Name) }

        $scriptParameters = [string]::Empty
        if ($parameters) {
            $scriptParameters = '$' + ($invokeMethod.GetParameters().Name -join ', $')
        }

        # Allow access to parameters in the following ways:
        # - By the name given to them by the delegate's invoke method
        # - $args
        # - $PSItem/$_  (first parameter only)
        $preparedScript =
            'param({0}) process {{ return {{ {1} }}.InvokeReturnAsIs($PSBoundParameters.Values) }}' -f
            $scriptParameters,
            $function

        # Prepare variable and constant expressions.
        $scriptText = [Expression]::Constant($preparedScript, [string])
        $ps         = [Expression]::Variable([powershell], 'ps')
        $collectionResultType = [Collection[psobject]]
        if ($returnType -ne [void] -and $returnType -ne [Collection[psobject]]) {
            $collectionResultType = [Collection`1].MakeGenericType($returnType)
        }

        $result     = [Expression]::Variable($collectionResultType, 'result')
        $psInput    = [Expression]::Variable([Object[]], 'psInput')
        $guid       = [Expression]::Constant($factory.InstanceId, [guid])
        $pool       = [Expression]::Property(
            [Expression]::Property(
                [Expression]::Property($null, [PSTaskFactory], 'Instances'),
                'Item',
                $guid),
            'RunspacePool')

        # Group the expressions for the body by creating them in a scriptblock.
        [Expression[]]$expressions = & {
            [Expression]::Assign($ps, [Expression]::Call([powershell], 'Create', @(), @()))
            [Expression]::Assign([Expression]::Property($ps, 'RunspacePool'), $pool)
            [Expression]::Call($ps, 'AddScript', @(), $scriptText)

            foreach ($parameter in $parameters) {
                [Expression]::Call($ps, 'AddArgument', @(), $parameter)
            }

            $invokeArgs = @()
            if ($parameters) {
                [Expression]::Assign(
                    $psInput,
                    [Expression]::NewArrayInit([object], $parameters[0] -as [Expression[]]))

                $invokeArgs = @($psInput)
            }

            $invokeTypeArgs = @()
            if ($returnType -ne [void] -and $returnType -ne [Collection[psobject]]) {
                $invokeTypeArgs = @($returnType)
            }

            [Expression]::Assign($result, [Expression]::Call($ps, 'Invoke', $invokeTypeArgs, $invokeArgs))
            [Expression]::Call($ps, 'Dispose', @(), @())
            if ($returnType -ne [void]) {
                [Expression]::Call(
                    [LanguagePrimitives],
                    'ConvertTo',
                    @($returnType),
                    $result -as [Expression[]])
            } else {
                $result
            }
        }

        $block  = [Expression]::Block([ParameterExpression[]]($ps, $result, $psInput), $expressions)
        $lambda = [Expression]::Lambda(
            $delegateType,
            $block,
            $parameters -as [ParameterExpression[]])
        return $lambda.Compile()
    }
}

# A TaskFactory implementation that creates tasks that run scriptblocks in a runspace pool.
class PSTaskFactory : TaskFactory[Collection[psobject]] {
    hidden static [Dictionary[guid, PSTaskFactory]] $Instances = [Dictionary[guid, PSTaskFactory]]::new()

    hidden [RunspacePool] $RunspacePool;
    hidden [guid] $InstanceId;
    hidden [bool] $IsDisposed = $false;

    PSTaskFactory() : base() {
        $this.Initialize()
    }

    PSTaskFactory([CancellationToken] $cancellationToken) : base($cancellationToken) {
        $this.Initialize()
    }

    PSTaskFactory([TaskScheduler] $scheduler) : base($scheduler) {
        $this.Initialize()
    }

    PSTaskFactory(
        [TaskCreationOptions] $creationOptions,
        [TaskContinuationOptions] $continuationOptions)
        : base($creationOptions, $continuationOptions) {
        $this.Initialize()
    }

    PSTaskFactory(
        [CancellationToken] $cancellationToken,
        [TaskCreationOptions] $creationOptions,
        [TaskContinuationOptions] $continuationOptions,
        [TaskScheduler] $scheduler)
        : base($cancellationToken, $creationOptions, $continuationAction, $scheduler) {
        $this.Initialize()
    }

    hidden [void] Initialize() {
        $this.RunspacePool = [runspacefactory]::CreateRunspacePool(1, 4)
        $this.RunspacePool.Open()
        $this.InstanceId = [guid]::NewGuid()
        [PSTaskFactory]::Instances.Add($this.InstanceId, $this)
    }

    # Can't implement IDisposable while inheriting a generic class because of a parse error, need
    # to create an issue.
    [void] Dispose() {
        $this.AssertNotDisposed()
        [PSTaskFactory]::Instances.Remove($this.InstanceId)
        $this.RunspacePool.Dispose()
        $this.IsDisposed = $true
    }

    hidden [void] AssertNotDisposed() {
        if ($this.IsDisposed) {
            throw [InvalidOperationException]::new(
                'Cannot perform operation because object "PSTaskFactory" has already been disposed.')
        }
    }

    # Shortcut to AsyncOps.CreateAsyncDelegate
    hidden [MulticastDelegate] Wrap([scriptblock] $function, [type] $delegateType) {
        $this.AssertNotDisposed()
        return [AsyncOps]::CreateAsyncDelegate($function, $delegateType, $this)
    }

    # The remaining functions implement methods from TaskFactory. All of these methods call the base
    # method after wrapping the scriptblock to create a delegate that will work in tasks.
    [Task[Collection[psobject]]] ContinueWhenAll([Task[]] $tasks, [scriptblock] $continuationAction) {
        $this.AssertNotDisposed()
        $delegateType = [Func`2].MakeGenericType([Task[]], [Collection[psobject]])
        return [AsyncOps]::PrepareTask(
            ([TaskFactory[Collection[psobject]]]$this).ContinueWhenAll(
                $tasks,
                $this.Wrap($continuationAction, $delegateType),
                $this.CancellationToken,
                $this.ContinuationOptions,
                [TaskScheduler]::Current))
    }

    [Task[Collection[psobject]]] ContinueWhenAny([Task[]] $tasks, [scriptblock] $continuationAction) {
        $this.AssertNotDisposed()
        $delegateType = [Func`2].MakeGenericType([Task], [Collection[psobject]])
        return [AsyncOps]::PrepareTask(
            ([TaskFactory[Collection[psobject]]]$this).ContinueWhenAny(
                $tasks,
                $this.Wrap($continuationAction, $delegateType),
                $this.CancellationToken,
                $this.ContinuationOptions,
                [TaskScheduler]::Current))
    }

    [Task[Collection[psobject]]] StartNew([scriptblock] $function) {
        $this.AssertNotDisposed()
        return [AsyncOps]::PrepareTask(
            ([TaskFactory[Collection[psobject]]]$this).StartNew(
                $this.Wrap($function, [Func[Collection[psobject]]]),
                $this.CancellationToken,
                $this.CreationOptions,
                [TaskScheduler]::Current))
    }

    [Task[Collection[psobject]]] StartNew([scriptblock] $function, [object] $state) {
        $this.AssertNotDisposed()
        return [AsyncOps]::PrepareTask(
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
        if (-not $scriptblock) { return }
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
        if ($Task) { $taskList.Add($Task) }
    }
    end {
        if (-not $taskList.Count) { return }

        $finished = $false
        while (-not $finished) {
            $finished = $taskList.TrueForAll({
                param([Task]$task)

                $task.IsCompleted -or
                $task.IsCanceled -or
                $task.IsFaulted
            })
        }
        $taskList.ToArray().ForEach{
            if ($PSItem.IsFaulted -and $PSItem.Exception) {
                if (-not ($exception = $PSItem.Exception.InnerException)) {
                    $exception = $PSItem.Exception
                }
                $PSCmdlet.WriteError(
                    [ErrorRecord]::new(
                        $exception,
                        $exception.GetType().Name,
                        [ErrorCategory]::InvalidResult,
                        $PSItem))
            }
            $PSItem.Result
        }
    }
}

function ContinueWith {
    [CmdletBinding()]
    param(
        [scriptblock]
        $ContinuationAction,

        [switch]
        $WhenAny,

        [Parameter(ValueFromPipeline)]
        [Task]
        $Task
    )
    begin {
        $taskList = [List[Task]]::new()
    }
    process {
        if ($Task) { $taskList.Add($Task) }
    }
    end {
        if (-not $taskList.Count) { return }

        if ($WhenAny.IsPresent) {
            return [AsyncOps]::Factory.ContinueWhenAny($taskList, $ContinuationAction)
        }
        return [AsyncOps]::Factory.ContinueWhenAll($taskList, $ContinuationAction)
    }
}
