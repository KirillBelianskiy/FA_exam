namespace Arithmetic;

public sealed class KaratsubaMultiplier : IMultiplier
{
    private const int SimpleThreshold = 32;

    private readonly SimpleMultiplier _simpleMultiplier = new();

    public uint[] Multiply(uint[] left, uint[] right)
    {
        left = SimpleMultiplier.Trim(left);
        right = SimpleMultiplier.Trim(right);

        if (IsZero(left) || IsZero(right))
        {
            return [0];
        }

        return SimpleMultiplier.Trim(MultiplyCore(left, right));
    }

    private uint[] MultiplyCore(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right)
    {
        left = TrimSpan(left);
        right = TrimSpan(right);

        if (left.Length == 0 || right.Length == 0)
        {
            return [0];
        }

        if (Math.Min(left.Length, right.Length) <= SimpleThreshold)
        {
            return _simpleMultiplier.Multiply(left.ToArray(), right.ToArray());
        }

        int split = Math.Max(left.Length, right.Length) / 2;

        ReadOnlySpan<uint> lowLeft = left[..Math.Min(split, left.Length)];
        ReadOnlySpan<uint> highLeft = left.Length > split ? left[split..] : ReadOnlySpan<uint>.Empty;
        ReadOnlySpan<uint> lowRight = right[..Math.Min(split, right.Length)];
        ReadOnlySpan<uint> highRight = right.Length > split ? right[split..] : ReadOnlySpan<uint>.Empty;

        uint[] z0 = MultiplyCore(lowLeft, lowRight);
        uint[] z2 = MultiplyCore(highLeft, highRight);
        uint[] sumLeft = AddAbs(lowLeft, highLeft);
        uint[] sumRight = AddAbs(lowRight, highRight);
        uint[] z1 = MultiplyCore(sumLeft, sumRight);

        z1 = SubAbs(SubAbs(z1, z0), z2);

        uint[] result = new uint[Math.Max(z0.Length, Math.Max(z1.Length + split, z2.Length + split * 2)) + 1];
        AddShifted(result, z0, 0);
        AddShifted(result, z1, split);
        AddShifted(result, z2, split * 2);

        return SimpleMultiplier.Trim(result);
    }

    private static ReadOnlySpan<uint> TrimSpan(ReadOnlySpan<uint> digits)
    {
        int length = digits.Length;
        while (length > 0 && digits[length - 1] == 0)
        {
            length--;
        }

        return digits[..length];
    }

    private static uint[] AddAbs(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right)
    {
        int length = Math.Max(left.Length, right.Length);
        uint[] result = new uint[length + 1];
        ulong carry = 0;

        for (int i = 0; i < length; i++)
        {
            ulong sum = carry;
            if (i < left.Length)
            {
                sum += left[i];
            }

            if (i < right.Length)
            {
                sum += right[i];
            }

            result[i] = (uint)sum;
            carry = sum >> 32;
        }

        result[length] = (uint)carry;
        return SimpleMultiplier.Trim(result);
    }

    private static uint[] SubAbs(uint[] left, uint[] right)
    {
        uint[] result = new uint[left.Length];
        long borrow = 0;

        for (int i = 0; i < left.Length; i++)
        {
            long rightValue = i < right.Length ? right[i] : 0L;
            long diff = (long)left[i] - rightValue - borrow;
            if (diff < 0)
            {
                diff += 1L << 32;
                borrow = 1;
            }
            else
            {
                borrow = 0;
            }

            result[i] = (uint)diff;
        }

        return SimpleMultiplier.Trim(result);
    }

    private static void AddShifted(uint[] target, uint[] value, int shift)
    {
        ulong carry = 0;

        for (int i = 0; i < value.Length; i++)
        {
            ulong sum = (ulong)target[i + shift] + value[i] + carry;
            target[i + shift] = (uint)sum;
            carry = sum >> 32;
        }

        int index = shift + value.Length;
        while (carry != 0)
        {
            ulong sum = target[index] + carry;
            target[index] = (uint)sum;
            carry = sum >> 32;
            index++;
        }
    }

    private static bool IsZero(uint[] digits)
    {
        return digits.Length == 0 || digits.Length == 1 && digits[0] == 0;
    }
}
