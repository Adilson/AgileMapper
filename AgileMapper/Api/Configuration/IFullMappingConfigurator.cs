﻿namespace AgileObjects.AgileMapper.Api.Configuration
{
    /// <summary>
    /// Provides options for configuring mappings from and to a given source and target type.
    /// </summary>
    /// <typeparam name="TSource">The source type to which the configuration should apply.</typeparam>
    /// <typeparam name="TTarget">The target type to which the configuration should apply.</typeparam>
    public interface IFullMappingConfigurator<TSource, TTarget> : IFullMappingSettings<TSource, TTarget>
    {
        /// <summary>
        /// Configure this mapper to perform an action before a different specified action.
        /// </summary>
        PreEventMappingConfigStartingPoint<TSource, TTarget> Before { get; }

        /// <summary>
        /// Configure this mapper to perform an action after a different specified action.
        /// </summary>
        PostEventMappingConfigStartingPoint<TSource, TTarget> After { get; }

        /// <summary>
        /// Configure a derived target type to which to map instances of the given derived source type.
        /// </summary>
        /// <typeparam name="TDerivedSource">
        /// The derived source type for which to configure a matching derived target type.
        /// </typeparam>
        /// <returns>A DerivedPairTargetTypeSpecifier with which to specify the matching derived target type.</returns>
        DerivedPairTargetTypeSpecifier<TSource, TDerivedSource, TTarget> Map<TDerivedSource>()
            where TDerivedSource : TSource;
    }
}