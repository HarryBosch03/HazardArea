using Runtime.Player;

namespace Runtime
{
    public static class Extensions
    {
        public static bool IsEmpty(this ItemStack stack) => stack.type == null || stack.amount == 0;
    }
}