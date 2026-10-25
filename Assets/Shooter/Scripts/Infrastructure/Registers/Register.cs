using System;

namespace Shooter.Infrastructure
{
    public sealed class Register
    {
        private const int InitialCapacity = 4;

        private readonly Type memberType;

        private IShooterBehaviour[] members;
        private int count;

        public Register(Type memberType)
        {
            this.memberType = memberType;
            members = (IShooterBehaviour[])Array.CreateInstance(memberType, InitialCapacity);
        }

        public ReadOnlySpan<T> GetMembers<T>() where T : class, IShooterBehaviour
        {
            return new ReadOnlySpan<T>((T[])members, 0, count);
        }

        public void AddMember(IShooterBehaviour member)
        {
            if (count == members.Length)
            {
                var grownMembers = (IShooterBehaviour[])Array.CreateInstance(memberType, members.Length * 2);
                Array.Copy(members, grownMembers, count);
                members = grownMembers;
            }

            members[count] = member;
            count++;
        }

        public void RemoveMember(IShooterBehaviour member)
        {
            for (int i = 0; i < count; i++)
            {
                if (!ReferenceEquals(members[i], member)) continue;

                count--;
                members[i] = members[count];
                members[count] = null;
                return;
            }
        }
    }
}
