﻿namespace AgileObjects.AgileMapper.Configuration
{
    using System;
#if !NET_STANDARD
    using System.Diagnostics.CodeAnalysis;
#endif
    using System.Globalization;
    using System.Linq;
#if NET_STANDARD
    using System.Reflection;
#endif
    using Members;
    using ReadableExpressions.Extensions;

    internal class DerivedTypePair : UserConfiguredItemBase
    {
        public DerivedTypePair(
            MappingConfigInfo configInfo,
            Type derivedSourceType,
            Type derivedTargetType)
            : base(configInfo)
        {
            DerivedSourceType = derivedSourceType;
            DerivedTargetType = derivedTargetType;
        }

        #region Factory Method

        public static DerivedTypePair For<TDerivedSource, TTarget, TDerivedTarget>(MappingConfigInfo configInfo)
        {
            ThrowIfInvalidSourceType<TDerivedSource>(configInfo);
            ThrowIfInvalidTargetType<TTarget, TDerivedTarget>();
            ThrowIfPairingIsUnnecessary<TDerivedSource, TDerivedTarget>(configInfo.ForTargetType<TTarget>());

            return new DerivedTypePair(configInfo, typeof(TDerivedSource), typeof(TDerivedTarget));
        }

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private static void ThrowIfInvalidSourceType<TDerivedSource>(MappingConfigInfo configInfo)
        {
            if ((configInfo.SourceType == typeof(TDerivedSource)) && !configInfo.HasCondition)
            {
                throw new MappingConfigurationException("A derived source type must be specified.");
            }
        }

        private static void ThrowIfInvalidTargetType<TTarget, TDerivedTarget>()
        {
            if (typeof(TTarget) == typeof(TDerivedTarget))
            {
                throw new MappingConfigurationException("A derived target type must be specified.");
            }
        }

        private static void ThrowIfPairingIsUnnecessary<TDerivedSource, TDerivedTarget>(MappingConfigInfo configInfo)
        {
            var mapperData = configInfo
                .Clone()
                .ForSourceType<TDerivedSource>()
                .ToMapperData();

            var matchingAutoTypePairing = configInfo
                .MapperContext
                .UserConfigurations
                .DerivedTypes
                .GetDerivedTypePairsFor(mapperData, configInfo.MapperContext)
                .FirstOrDefault(tp =>
                    !tp.HasConfiguredCondition && (tp.DerivedTargetType == typeof(TDerivedTarget)));

            if (matchingAutoTypePairing != null)
            {
                throw new MappingConfigurationException(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} is automatically mapped to {1} when mapping {2} to {3} and does not need to be configured.",
                    matchingAutoTypePairing.DerivedSourceType.GetFriendlyName(),
                    matchingAutoTypePairing.DerivedTargetType.GetFriendlyName(),
                    configInfo.SourceType.GetFriendlyName(),
                    configInfo.TargetType.GetFriendlyName()));
            }
        }

        #endregion

        public Type DerivedSourceType { get; }

        public Type DerivedTargetType { get; }

        public override bool AppliesTo(IBasicMapperData mapperData)
            => DerivedSourceType.IsAssignableFrom(mapperData.SourceType) && base.AppliesTo(mapperData);

        #region ExcludeFromCodeCoverage
#if !NET_STANDARD
        [ExcludeFromCodeCoverage]
#endif
        #endregion
        public override string ToString()
        {
            var rootSourceType = ConfigInfo.SourceType.GetFriendlyName();
            var rootTargetType = TargetTypeName;
            var derivedSourceType = DerivedSourceType.GetFriendlyName();
            var derivedTargetType = DerivedTargetType.GetFriendlyName();

            return $"{rootSourceType} -> {rootTargetType} > {derivedSourceType} -> {derivedTargetType}";
        }
    }
}