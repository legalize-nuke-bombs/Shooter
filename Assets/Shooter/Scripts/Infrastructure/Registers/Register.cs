using System;

namespace Shooter.Infrastructure
{
    public sealed class Register
    {
        private const int InitialCapacity = 4;

        private readonly Type memberType;

        private object[] members;
        private int[] executionOrders;
        private int count;

        public Register(Type memberType)
        {
            this.memberType = memberType;
            members = (object[])Array.CreateInstance(memberType, InitialCapacity);
            executionOrders = new int[InitialCapacity];
        }

        public ReadOnlySpan<T> GetMembers<T>() where T : class
        {
            return new ReadOnlySpan<T>((T[])members, 0, count);
        }

        // Members stay sorted by execution order, equal orders by arrival, so a walk over a register calls them in the
        // order Unity would call their own Update
        public void AddMember(IShooterBehaviour member, int executionOrder)
        {
            if (count == members.Length)
            {
                var grownMembers = (object[])Array.CreateInstance(memberType, members.Length * 2);
                Array.Copy(members, grownMembers, count);
                members = grownMembers;
                Array.Resize(ref executionOrders, members.Length);
            }

            int index = count;
            while (index > 0 && executionOrders[index - 1] > executionOrder)
            {
                index--;
            }

            Array.Copy(members, index, members, index + 1, count - index);
            Array.Copy(executionOrders, index, executionOrders, index + 1, count - index);
            members[index] = member;
            executionOrders[index] = executionOrder;
            count++;
        }

        public void RemoveMember(IShooterBehaviour member)
        {
            for (int i = 0; i < count; i++)
            {
                if (!ReferenceEquals(members[i], member)) continue;

                count--;
                Array.Copy(members, i + 1, members, i, count - i);
                Array.Copy(executionOrders, i + 1, executionOrders, i, count - i);
                members[count] = null;
                return;
            }
        }
    }
}
