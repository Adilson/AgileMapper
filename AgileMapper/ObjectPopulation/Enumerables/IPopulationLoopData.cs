﻿namespace AgileObjects.AgileMapper.ObjectPopulation.Enumerables
{
    using System;
    using System.Linq.Expressions;
    using Extensions;

    internal interface IPopulationLoopData
    {
        LabelTarget ContinueLoopTarget { get; }

        Expression LoopExitCheck { get; }

        Expression GetSourceElementValue();

        Expression GetElementMapping(IObjectMappingData enumerableMappingData);

        Expression Adapt(LoopExpression loop);
    }

    internal static class PopulationLoopDataExtensions
    {
        public static Expression BuildPopulationLoop<TLoopData>(
            this TLoopData loopData,
            EnumerablePopulationBuilder builder,
            IObjectMappingData mappingData,
            Func<TLoopData, IObjectMappingData, Expression> elementPopulationFactory)
            where TLoopData : IPopulationLoopData
        {
            var breakLoop = Expression.Break(Expression.Label(typeof(void), "Break"));

            var elementPopulation = elementPopulationFactory.Invoke(loopData, mappingData);

            var loopBody = Expression.Block(
                Expression.IfThen(loopData.LoopExitCheck, breakLoop),
                elementPopulation,
                Expression.PreIncrementAssign(builder.Counter));

            var populationLoop = Expression.Loop(loopBody, breakLoop.Target, loopData.ContinueLoopTarget);
            var adaptedLoop = loopData.Adapt(populationLoop);

            var population = Expression.Block(
                new[] { builder.Counter },
                builder.Counter.AssignTo(0.ToConstantExpression()),
                adaptedLoop);

            return population;
        }
    }
}