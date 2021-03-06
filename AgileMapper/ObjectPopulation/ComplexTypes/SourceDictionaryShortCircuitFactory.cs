namespace AgileObjects.AgileMapper.ObjectPopulation.ComplexTypes
{
    using System.Collections.Generic;
    using System.Linq.Expressions;
#if NET_STANDARD
    using System.Reflection;
#endif
    using DataSources;
    using Extensions;
    using Members;
    using ReadableExpressions.Extensions;

    internal class SourceDictionaryShortCircuitFactory : ISourceShortCircuitFactory
    {
        public static readonly ISourceShortCircuitFactory Instance = new SourceDictionaryShortCircuitFactory();

        public bool IsFor(ObjectMapperData mapperData)
        {
            if (mapperData.Context.IsStandalone)
            {
                // A standalone context is either the root, in which case we don't 
                // check the dictionary for a matching object entry, or in a mapper 
                // completing a .Map() call, in which case we already have:
                return false;
            }

            if (!mapperData.SourceMemberIsStringKeyedDictionary(out var dictionarySourceMember))
            {
                return false;
            }

            if (dictionarySourceMember.IsEntireDictionaryMatch)
            {
                // The dictionary has been matched to a target complex type
                // member, so should only be used to get that member's member
                // values, not a value for the member itself
                return false;
            }

            return dictionarySourceMember.HasObjectEntries ||
                   !dictionarySourceMember.ValueType.IsSimple();
        }

        public Expression GetShortCircuit(IObjectMappingData mappingData)
        {
            var mapperData = mappingData.MapperData;

            var dictionaryVariables = new DictionaryEntryVariablePair(mapperData);
            var foundValueNonNull = dictionaryVariables.Value.GetIsNotDefaultComparison();

            var entryExistsTest = GetEntryExistsTest(dictionaryVariables);

            var mapValueCall = GetMapValueCall(dictionaryVariables.Value, mapperData);
            var fallbackValue = GetFallbackValue(mappingData);

            if (mapperData.TargetMember.IsRecursionRoot())
            {
                AdjustForStandaloneContext(ref mapValueCall, ref fallbackValue, mapperData);
            }

            var valueMappingOrFallback = Expression.Condition(foundValueNonNull, mapValueCall, fallbackValue);
            var returnMapValueResult = Expression.Return(mapperData.ReturnLabelTarget, valueMappingOrFallback);
            var ifEntryExistsShortCircuit = Expression.IfThen(entryExistsTest, returnMapValueResult);

            return Expression.Block(dictionaryVariables.Variables, ifEntryExistsShortCircuit);
        }

        private static Expression GetEntryExistsTest(DictionaryEntryVariablePair dictionaryVariables)
        {
            var returnLabel = Expression.Label(typeof(bool), "Return");
            var returnFalse = Expression.Return(returnLabel, false.ToConstantExpression());

            var ifKeyNotFoundReturnFalse = dictionaryVariables.GetKeyNotFoundShortCircuit(returnFalse);
            var valueAssignment = dictionaryVariables.GetEntryValueAssignment();
            var returnTrue = Expression.Label(returnLabel, true.ToConstantExpression());

            if (dictionaryVariables.HasConstantTargetMemberKey)
            {
                return Expression.Block(ifKeyNotFoundReturnFalse, valueAssignment, returnTrue);
            }

            var keyAssignment = dictionaryVariables.GetNonConstantKeyAssignment();

            return Expression.Block(
                keyAssignment,
                ifKeyNotFoundReturnFalse,
                valueAssignment,
                returnTrue);
        }

        private static MethodCallExpression GetMapValueCall(Expression sourceValue, IMemberMapperData mapperData)
        {
            if (mapperData.TargetMemberIsEnumerableElement())
            {
                return mapperData.Parent.GetMapCall(sourceValue);
            }

            return mapperData.Parent.GetMapCall(sourceValue, mapperData.TargetMember, dataSourceIndex: 0);
        }

        private static Expression GetFallbackValue(IObjectMappingData mappingData)
        {
            if (mappingData.MapperData.TargetMemberIsEnumerableElement())
            {
                return mappingData.MapperData.TargetMember.Type.ToDefaultExpression();
            }

            return mappingData.MappingContext
                .RuleSet
                .FallbackDataSourceFactory
                .Create(mappingData.MapperData)
                .Value;
        }

        private static void AdjustForStandaloneContext(
            ref MethodCallExpression mapValueCall,
            ref Expression fallbackValue,
            IMemberMapperData mapperData)
        {
            var parentMappingTypes = mapperData.Parent.MappingDataObject.Type.GetGenericArguments();
            var parentContextAccess = mapperData.GetAppropriateMappingContextAccess(parentMappingTypes);
            var typedParentContextAccess = mapperData.GetTypedContextAccess(parentContextAccess, parentMappingTypes);
            var parentTargetAccess = mapperData.GetTargetAccess(parentContextAccess, mapperData.TargetType);

            var replacements = new Dictionary<Expression, Expression>(2)
            {
                [mapValueCall.GetSubject()] = typedParentContextAccess,
                [mapValueCall.Arguments[1]] = parentTargetAccess
            };

            mapValueCall = mapValueCall.Replace(replacements);

            if (fallbackValue.NodeType != ExpressionType.Default)
            {
                fallbackValue = parentTargetAccess;
            }
        }
    }
}