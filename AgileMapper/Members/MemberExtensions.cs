﻿namespace AgileObjects.AgileMapper.Members
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using Extensions;

    internal static class MemberExtensions
    {
        #region AccessFactoriesByMemberType

        private static readonly Dictionary<MemberType, Func<Expression, Member, Expression>> _accessFactoriesByMemberType =
            new Dictionary<MemberType, Func<Expression, Member, Expression>>
            {
                { MemberType.Field, (instance, member) => Expression.Field(instance, FindMember<FieldInfo>(member)) },
                { MemberType.Property, (instance, member) => Expression.Property(instance, FindMember<PropertyInfo>(member)) },
                { MemberType.GetMethod, (instance, member) => Expression.Call(instance, FindMember<MethodInfo>(member)) }
            };

        private static TMemberInfo FindMember<TMemberInfo>(Member member)
            where TMemberInfo : MemberInfo
        {
            return (TMemberInfo)
                member.DeclaringType
                    .GetMember(member.Name, BindingFlags.Public | BindingFlags.Instance)
                    .First();
        }

        #endregion

        public static Expression GetAccess(this Member member, Expression instance)
        {
            if (!member.ExistingValueCanBeChecked)
            {
                return Expression.Default(member.Type);
            }

            if (!member.DeclaringType.IsAssignableFrom(instance.Type))
            {
                instance = Expression.Convert(instance, member.DeclaringType);
            }

            var accessFactory = _accessFactoriesByMemberType[member.MemberType];
            var access = accessFactory.Invoke(instance, member);

            return access;
        }

        public static Expression GetQualifiedAccess(this IEnumerable<Member> memberChain, Expression instance)
            => memberChain.Aggregate(instance, (accessSoFar, member) => member.GetAccess(accessSoFar));

        #region PopulationFactoriesByMemberType

        private delegate Expression PopulationFactory(Expression instance, Member member, Expression value);

        private static readonly Dictionary<MemberType, PopulationFactory> _populationFactoriesByMemberType =
            new Dictionary<MemberType, PopulationFactory>
            {
                { MemberType.Field, AssignMember },
                { MemberType.Property, AssignMember },
                { MemberType.SetMethod, CallSetMethod }
            };


        private static Expression AssignMember(Expression instance, Member targetMember, Expression value)
            => Expression.Assign(targetMember.GetAccess(instance), value);

        private static Expression CallSetMethod(Expression instance, Member targetMember, Expression value)
            => Expression.Call(instance, targetMember.Name, Constants.NoTypeArguments, value);

        #endregion

        public static Expression GetPopulation(this Member targetMember, Expression instance, Expression value)
        {
            var populationFactory = _populationFactoriesByMemberType[targetMember.MemberType];
            var population = populationFactory.Invoke(instance, targetMember, value);

            return population;
        }

        public static IQualifiedMember ToTargetMember(this Expression memberAccessExpression, MemberFinder memberFinder)
            => CreateMember(memberAccessExpression, Member.RootTarget, memberFinder.GetTargetMembers);

        internal static IQualifiedMember CreateMember(
            Expression memberAccessExpression,
            Func<Type, Member> rootMemberFactory,
            Func<Type, IEnumerable<Member>> membersFactory)
        {
            var expression = memberAccessExpression;
            var memberAccesses = new List<Expression>();

            while (expression.NodeType != ExpressionType.Parameter)
            {
                var memberExpression = expression.GetMemberAccess();
                memberAccesses.Insert(0, memberExpression);
                expression = memberExpression.GetParentOrNull();
            }

            var rootMember = rootMemberFactory.Invoke(expression.Type);
            var parentMember = rootMember;

            var memberChain = memberAccesses
                .Select(memberAccess =>
                {
                    var memberName = memberAccess.GetMemberName();
                    var members = membersFactory.Invoke(parentMember.Type);
                    var member = members.FirstOrDefault(m => m.Name == memberName);

                    parentMember = member;

                    return member;
                })
                .ToList();

            memberChain.Insert(0, rootMember);

            return QualifiedMember.From(memberChain.ToArray());
        }
    }
}
