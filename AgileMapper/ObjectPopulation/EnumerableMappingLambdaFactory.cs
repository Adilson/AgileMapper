namespace AgileObjects.AgileMapper.ObjectPopulation
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using Extensions;

    internal class EnumerableMappingLambdaFactory<TSource, TTarget, TInstance>
        : ObjectMappingLambdaFactoryBase<TSource, TTarget, TInstance>
    {
        public static readonly ObjectMappingLambdaFactoryBase<TSource, TTarget, TInstance> Instance =
            new EnumerableMappingLambdaFactory<TSource, TTarget, TInstance>();

        protected override IEnumerable<Expression> GetShortCircuitReturns(GotoExpression returnNull, IObjectMappingContext omc)
            => Enumerable.Empty<Expression>();

        protected override Expression GetObjectResolution(IObjectMappingContext omc)
            => EnumerableTypes.GetEnumerableVariableValue(omc);

        protected override IEnumerable<Expression> GetObjectPopulation(Expression instanceVariableValue, IObjectMappingContext omc)
        {
            yield return omc.MappingContext.RuleSet.EnumerablePopulationStrategy.GetPopulation(instanceVariableValue, omc);
        }

        protected override Expression GetReturnValue(Expression instanceVariableValue, IObjectMappingContext omc)
        {
            if (omc.TargetMember.Type.IsAssignableFrom(omc.InstanceVariable.Type))
            {
                return omc.InstanceVariable;
            }

            return omc.InstanceVariable.WithToArrayCall();
        }
    }
}