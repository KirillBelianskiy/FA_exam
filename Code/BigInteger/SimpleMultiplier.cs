namespace Arithmetic;

public sealed class SimpleMultiplier : IMultiplier
{
    public uint[] Multiply(uint[] left, uint[] right)
    {
        left = Trim(left);
        right = Trim(right);

        if (IsZero(left) || IsZero(right))
        {
            return [0];
        }

        uint[] result = new uint[left.Length + right.Length];

        for (int i = 0; i < left.Length; i++)
        {
            ulong carry = 0;

            for (int j = 0; j < right.Length; j++)
            {
                ulong current = result[i + j] + carry + (ulong)left[i] * right[j];
                result[i + j] = (uint)current;
                carry = current >> 32;
            }

            int index = i + right.Length;
            while (carry != 0)
            {
                ulong current = result[index] + carry;
                result[index] = (uint)current;
                carry = current >> 32;
                index++;
            }
        }

        return Trim(result);
    }

    internal static uint[] Trim(uint[] digits)
    {
        if (digits.Length == 0)
        {
            return [0];
        }

        int length = digits.Length;
        while (length > 1 && digits[length - 1] == 0)
        {
            length--;
        }

        if (length == digits.Length)
        {
            return digits;
        }

        uint[] result = new uint[length];
        Array.Copy(digits, result, length);
        return result;
    }

    private static bool IsZero(uint[] digits)
    {
        return digits.Length == 0 || digits.Length == 1 && digits[0] == 0;
    }
}
