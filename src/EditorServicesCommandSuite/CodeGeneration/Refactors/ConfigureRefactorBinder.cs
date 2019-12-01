using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using EditorServicesCommandSuite.Utility;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    internal sealed class ConfigureRefactorBinder : CallSiteBinder
    {
        private static readonly MethodInfo s_tryGetSetting =
            typeof(Settings).GetMethod(
                nameof(Settings.TryGetSetting),
                BindingFlags.NonPublic | BindingFlags.Static,
                null,
                new[] { typeof(string), typeof(Type), typeof(object).MakeByRefType() },
                new[] { new ParameterModifier(3) });

        private static readonly ConcurrentDictionary<Type, CallSite<Action<CallSite, RefactorConfiguration>>> s_binderCache =
            new ConcurrentDictionary<Type, CallSite<Action<CallSite, RefactorConfiguration>>>();

        private readonly Type _configType;

        private ConfigureRefactorBinder(Type configType)
        {
            _configType = configType;
        }

        public override Expression Bind(
            object[] args,
            ReadOnlyCollection<ParameterExpression> parameters,
            LabelTarget returnLabel)
        {
            ParameterExpression tempVar = Expression.Variable(typeof(object));
            var expressions = new List<Expression>();
            foreach (PropertyInfo property in _configType.GetProperties())
            {
                var fromSetting = property.GetCustomAttribute<DefaultFromSettingAttribute>(inherit: true);
                if (fromSetting == null)
                {
                    continue;
                }

                expressions.Add(
                    Expression.IfThen(
                        Expression.Call(
                            s_tryGetSetting,
                            Expression.Constant(fromSetting.Key, typeof(string)),
                            Expression.Constant(property.PropertyType, typeof(Type)),
                            tempVar),
                        Expression.Assign(
                            Expression.Property(
                                Expression.Convert(parameters[0], _configType),
                                property),
                            Expression.Convert(tempVar, property.PropertyType))));
            }

            if (expressions.Count == 0)
            {
                return Expression.Return(returnLabel);
            }

            expressions.Add(Expression.Return(returnLabel));
            return Expression.Block(new[] { tempVar }, expressions);
        }

        internal static CallSite<Action<CallSite, RefactorConfiguration>> Get(RefactorConfiguration configuration)
        {
            return s_binderCache.GetOrAdd(
                configuration.GetType(),
                type => CallSite<Action<CallSite, RefactorConfiguration>>.Create(new ConfigureRefactorBinder(type)));
        }
    }
}
