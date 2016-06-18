namespace AgileObjects.AgileMapper.Members
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Extensions;

    [DebuggerDisplay("{FullName}")]
    internal class QualifiedMemberName
    {
        private readonly MemberName _rootNamePart;
        private readonly MemberName[] _nameParts;
        private readonly IEnumerable<string> _allJoinedNames;

        public QualifiedMemberName(MemberName[] nameParts)
        {
            FullName = GetFullName(nameParts);

            _rootNamePart = nameParts[0];
            _nameParts = new MemberName[nameParts.Length - 1]; // <- Don't bother with the root property
            Array.Copy(nameParts, 1, _nameParts, 0, _nameParts.Length);

            _allJoinedNames = _nameParts
                .Select(np => np.AllNames)
                .CartesianProduct()
                .Select(p => string.Join(string.Empty, p))
                .Where(name => name != string.Empty)
                .ToArray();
        }

        public static string GetFullName(IEnumerable<MemberName> nameParts)
            => string.Join(string.Empty, nameParts.Select(np => np?.JoiningName));

        public string FullName { get; }

        public bool IsRootOf(QualifiedMemberName otherQualifiedName)
        {
            if (_nameParts.Length == 0)
            {
                return true;
            }

            return otherQualifiedName._allJoinedNames
                .Any(otherName => _allJoinedNames
                    .Any(name => otherName.StartsWith(name, StringComparison.OrdinalIgnoreCase)));
        }

        public bool Matches(QualifiedMemberName otherQualifiedName)
        {
            if (_allJoinedNames.Intersect(otherQualifiedName._allJoinedNames, CaseInsensitiveStringComparer.Instance).Any())
            {
                return true;
            }

            if (_nameParts.Length != otherQualifiedName._nameParts.Length)
            {
                return false;
            }

            return _nameParts
                .Where((t, i) => !t.Matches(otherQualifiedName._nameParts[i]))
                .None();
        }

        public QualifiedMemberName Append(MemberName memberName)
            => new QualifiedMemberName(GetNameParts(memberName));

        private MemberName[] GetNameParts(MemberName memberName)
        {
            var nameParts = new MemberName[_nameParts.Length + 2];

            nameParts[0] = _rootNamePart;
            _nameParts.CopyTo(nameParts, 1);
            nameParts[_nameParts.Length + 1] = memberName;

            return nameParts;
        }
    }
}