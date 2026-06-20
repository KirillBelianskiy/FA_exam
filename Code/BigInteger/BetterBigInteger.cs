using System.Runtime.InteropServices;
using System.Text;

namespace Arithmetic;

public sealed class BetterBigInteger
    : IComparable<BetterBigInteger>, IEquatable<BetterBigInteger>
{
    private const int MinRadix = 2;
    private const int MaxRadix = 36;
    private const int KaratsubaThreshold = 32;
    private const int FftThreshold = 512;

    private static readonly IMultiplier SimpleMultiplier = new SimpleMultiplier();
    private static readonly IMultiplier KaratsubaMultiplier = new KaratsubaMultiplier();
    private static readonly IMultiplier FftMultiplier = new FftMultiplier();
    private static readonly char[] DigitChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();

    private int _signBit;
    private uint _smallValue;
    private uint[]? _data;

    public BetterBigInteger(uint[] digits, bool isNegative)
    {
        ArgumentNullException.ThrowIfNull(digits);
        SetMagnitude((uint[])digits.Clone(), isNegative);
    }

    public BetterBigInteger(IEnumerable<uint> digits, bool isNegative)
    {
        ArgumentNullException.ThrowIfNull(digits);
        SetMagnitude(digits.ToArray(), isNegative);
    }

    public BetterBigInteger(string value, int radix)
    {
        ValidateRadix(radix);
        ArgumentNullException.ThrowIfNull(value);

        string trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            throw new FormatException("The value is empty.");
        }

        bool isNegative = false;
        int index = 0;

        if (trimmed[0] == '+' || trimmed[0] == '-')
        {
            isNegative = trimmed[0] == '-';
            index = 1;
        }

        if (index == trimmed.Length)
        {
            throw new FormatException("The value does not contain digits.");
        }

        uint[] magnitude = [0];

        for (; index < trimmed.Length; index++)
        {
            int digit = GetDigitValue(trimmed[index]);
            if (digit < 0 || digit >= radix)
            {
                throw new FormatException($"Invalid digit '{trimmed[index]}' for radix {radix}.");
            }

            magnitude = AddMagnitudeUInt(MultiplyMagnitudeByUInt(magnitude, (uint)radix), (uint)digit);
        }

        SetMagnitude(magnitude, isNegative);
    }

    private BetterBigInteger(uint[] magnitude, int signBit)
    {
        SetMagnitude(magnitude, signBit != 0);
    }

    public bool IsNegative => _signBit != 0;

    public ReadOnlySpan<uint> GetDigits()
    {
        if (_data is not null)
        {
            return _data;
        }

        return MemoryMarshal.CreateReadOnlySpan(ref _smallValue, 1);
    }

    public string ToString(int radix)
    {
        ValidateRadix(radix);

        if (IsZero)
        {
            return "0";
        }

        uint[] current = ToMagnitudeArray();
        StringBuilder builder = new();

        while (!IsZeroMagnitude(current))
        {
            current = DivideMagnitudeByUInt(current, (uint)radix, out uint remainder);
            builder.Append(DigitChars[remainder]);
        }

        if (IsNegative)
        {
            builder.Append('-');
        }

        Reverse(builder);
        return builder.ToString();
    }

    public override string ToString()
    {
        return ToString(10);
    }

    public int CompareTo(BetterBigInteger? other)
    {
        if (other is null)
        {
            return 1;
        }

        if (_signBit != other._signBit)
        {
            return _signBit == 0 ? 1 : -1;
        }

        int absoluteComparison = CompareMagnitude(GetDigits(), other.GetDigits());
        return IsNegative ? -absoluteComparison : absoluteComparison;
    }

    public bool Equals(BetterBigInteger? other)
    {
        if (other is null)
        {
            return false;
        }

        return _signBit == other._signBit && CompareMagnitude(GetDigits(), other.GetDigits()) == 0;
    }

    public override bool Equals(object? obj)
    {
        return obj is BetterBigInteger other && Equals(other);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();
        hash.Add(_signBit);

        ReadOnlySpan<uint> digits = GetDigits();
        for (int i = 0; i < digits.Length; i++)
        {
            hash.Add(digits[i]);
        }

        return hash.ToHashCode();
    }

    public static bool operator ==(BetterBigInteger? left, BetterBigInteger? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return left.Equals(right);
    }

    public static bool operator !=(BetterBigInteger? left, BetterBigInteger? right)
    {
        return !(left == right);
    }

    public static bool operator <(BetterBigInteger left, BetterBigInteger right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return left.CompareTo(right) < 0;
    }

    public static bool operator >(BetterBigInteger left, BetterBigInteger right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return left.CompareTo(right) > 0;
    }

    public static bool operator <=(BetterBigInteger left, BetterBigInteger right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >=(BetterBigInteger left, BetterBigInteger right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return left.CompareTo(right) >= 0;
    }

    public static BetterBigInteger operator +(BetterBigInteger left, BetterBigInteger right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        if (left._signBit == right._signBit)
        {
            return FromMagnitude(AddMagnitude(left.GetDigits(), right.GetDigits()), left._signBit);
        }

        int comparison = CompareMagnitude(left.GetDigits(), right.GetDigits());
        if (comparison == 0)
        {
            return Zero();
        }

        if (comparison > 0)
        {
            return FromMagnitude(SubtractMagnitude(left.GetDigits(), right.GetDigits()), left._signBit);
        }

        return FromMagnitude(SubtractMagnitude(right.GetDigits(), left.GetDigits()), right._signBit);
    }

    public static BetterBigInteger operator -(BetterBigInteger left, BetterBigInteger right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return left + (-right);
    }

    public static BetterBigInteger operator -(BetterBigInteger value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.IsZero)
        {
            return Zero();
        }

        return FromMagnitude(value.ToMagnitudeArray(), value._signBit == 0 ? 1 : 0);
    }

    public static BetterBigInteger operator *(BetterBigInteger left, BetterBigInteger right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        if (left.IsZero || right.IsZero)
        {
            return Zero();
        }

        uint[] leftDigits = left.ToMagnitudeArray();
        uint[] rightDigits = right.ToMagnitudeArray();
        IMultiplier multiplier = SelectMultiplier(Math.Max(leftDigits.Length, rightDigits.Length));
        uint[] result = multiplier.Multiply(leftDigits, rightDigits);
        int signBit = left._signBit ^ right._signBit;
        return FromMagnitude(result, signBit);
    }

    public static BetterBigInteger operator /(BetterBigInteger left, BetterBigInteger right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        DivRem(left, right, out BetterBigInteger quotient, out _);
        return quotient;
    }

    public static BetterBigInteger operator %(BetterBigInteger left, BetterBigInteger right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        DivRem(left, right, out _, out BetterBigInteger remainder);
        return remainder;
    }

    public static BetterBigInteger operator ~(BetterBigInteger value)
    {
        ArgumentNullException.ThrowIfNull(value);

        int length = value.GetSignedWordLength() + 1;
        uint[] words = value.ToTwosComplement(length);

        for (int i = 0; i < words.Length; i++)
        {
            words[i] = ~words[i];
        }

        return FromTwosComplement(words);
    }

    public static BetterBigInteger operator &(BetterBigInteger left, BetterBigInteger right)
    {
        return ApplyBitwise(left, right, static (a, b) => a & b);
    }

    public static BetterBigInteger operator |(BetterBigInteger left, BetterBigInteger right)
    {
        return ApplyBitwise(left, right, static (a, b) => a | b);
    }

    public static BetterBigInteger operator ^(BetterBigInteger left, BetterBigInteger right)
    {
        return ApplyBitwise(left, right, static (a, b) => a ^ b);
    }

    public static BetterBigInteger operator <<(BetterBigInteger value, int shift)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (shift < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(shift));
        }

        if (shift == 0 || value.IsZero)
        {
            return FromMagnitude(value.ToMagnitudeArray(), value._signBit);
        }

        return FromMagnitude(ShiftLeftMagnitude(value.GetDigits(), shift), value._signBit);
    }

    public static BetterBigInteger operator >>(BetterBigInteger value, int shift)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (shift < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(shift));
        }

        if (shift == 0 || value.IsZero)
        {
            return FromMagnitude(value.ToMagnitudeArray(), value._signBit);
        }

        uint[] quotient = ShiftRightMagnitude(value.GetDigits(), shift, out bool hasRemainder);

        if (value.IsNegative && hasRemainder)
        {
            quotient = AddMagnitudeUInt(quotient, 1);
        }

        return FromMagnitude(quotient, value._signBit);
    }

    private bool IsZero => _data is null && _smallValue == 0;

    private static BetterBigInteger Zero()
    {
        return new BetterBigInteger([0], signBit: 0);
    }

    private static BetterBigInteger FromMagnitude(uint[] magnitude, int signBit)
    {
        return new BetterBigInteger(magnitude, signBit);
    }

    private void SetMagnitude(uint[] digits, bool isNegative)
    {
        digits = TrimMagnitude(digits);

        if (IsZeroMagnitude(digits))
        {
            _signBit = 0;
            _smallValue = 0;
            _data = null;
            return;
        }

        _signBit = isNegative ? 1 : 0;

        if (digits.Length == 1)
        {
            _smallValue = digits[0];
            _data = null;
            return;
        }

        _smallValue = 0;
        _data = digits;
    }

    private uint[] ToMagnitudeArray()
    {
        if (_data is not null)
        {
            return (uint[])_data.Clone();
        }

        return [_smallValue];
    }

    private static IMultiplier SelectMultiplier(int wordCount)
    {
        if (wordCount >= FftThreshold)
        {
            return FftMultiplier;
        }

        if (wordCount >= KaratsubaThreshold)
        {
            return KaratsubaMultiplier;
        }

        return SimpleMultiplier;
    }

    private static void DivRem(BetterBigInteger left, BetterBigInteger right, out BetterBigInteger quotient, out BetterBigInteger remainder)
    {
        if (right.IsZero)
        {
            throw new DivideByZeroException();
        }

        DivRemMagnitude(left.GetDigits(), right.GetDigits(), out uint[] quotientMagnitude, out uint[] remainderMagnitude);

        quotient = FromMagnitude(quotientMagnitude, left._signBit ^ right._signBit);
        remainder = FromMagnitude(remainderMagnitude, left._signBit);
    }

    private static void DivRemMagnitude(
        ReadOnlySpan<uint> dividend,
        ReadOnlySpan<uint> divisor,
        out uint[] quotient,
        out uint[] remainder)
    {
        dividend = TrimSpan(dividend);
        divisor = TrimSpan(divisor);

        if (divisor.Length == 0)
        {
            throw new DivideByZeroException();
        }

        int comparison = CompareMagnitude(dividend, divisor);
        if (comparison < 0)
        {
            quotient = [0];
            remainder = dividend.ToArray();
            return;
        }

        if (comparison == 0)
        {
            quotient = [1];
            remainder = [0];
            return;
        }

        if (divisor.Length == 1)
        {
            quotient = DivideMagnitudeByUInt(dividend.ToArray(), divisor[0], out uint rem);
            remainder = [rem];
            return;
        }

        int bitLength = GetBitLength(dividend);
        quotient = new uint[(bitLength + 31) / 32];
        uint[] currentRemainder = [0];

        // Binary long division is slower than Knuth division, but compact and dependable.
        for (int bit = bitLength - 1; bit >= 0; bit--)
        {
            currentRemainder = ShiftLeftMagnitude(currentRemainder, 1);
            if (GetBit(dividend, bit))
            {
                currentRemainder[0] |= 1;
            }

            if (CompareMagnitude(currentRemainder, divisor) >= 0)
            {
                currentRemainder = SubtractMagnitude(currentRemainder, divisor);
                SetBit(quotient, bit);
            }
        }

        quotient = TrimMagnitude(quotient);
        remainder = TrimMagnitude(currentRemainder);
    }

    private static BetterBigInteger ApplyBitwise(BetterBigInteger left, BetterBigInteger right, Func<uint, uint, uint> operation)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        int length = Math.Max(left.GetSignedWordLength(), right.GetSignedWordLength()) + 1;
        uint[] leftWords = left.ToTwosComplement(length);
        uint[] rightWords = right.ToTwosComplement(length);
        uint[] result = new uint[length];

        for (int i = 0; i < result.Length; i++)
        {
            result[i] = operation(leftWords[i], rightWords[i]);
        }

        return FromTwosComplement(result);
    }

    private int GetSignedWordLength()
    {
        ReadOnlySpan<uint> digits = GetDigits();
        int length = digits.Length;

        if (!IsNegative && (digits[^1] & 0x80000000) != 0)
        {
            length++;
        }

        if (IsNegative)
        {
            length++;
        }

        return Math.Max(1, length);
    }

    private uint[] ToTwosComplement(int length)
    {
        uint[] words = new uint[length];
        ReadOnlySpan<uint> digits = GetDigits();

        for (int i = 0; i < Math.Min(length, digits.Length); i++)
        {
            words[i] = digits[i];
        }

        if (!IsNegative)
        {
            return words;
        }

        // -m is encoded as bitwise-not(m - 1), equivalently not(m) + 1.
        for (int i = 0; i < words.Length; i++)
        {
            words[i] = ~words[i];
        }

        ulong carry = 1;
        for (int i = 0; i < words.Length && carry != 0; i++)
        {
            ulong sum = words[i] + carry;
            words[i] = (uint)sum;
            carry = sum >> 32;
        }

        return words;
    }

    private static BetterBigInteger FromTwosComplement(uint[] words)
    {
        if (words.Length == 0)
        {
            return Zero();
        }

        bool isNegative = (words[^1] & 0x80000000) != 0;
        uint[] magnitude = (uint[])words.Clone();

        if (!isNegative)
        {
            return FromMagnitude(magnitude, signBit: 0);
        }

        for (int i = 0; i < magnitude.Length; i++)
        {
            magnitude[i] = ~magnitude[i];
        }

        ulong carry = 1;
        for (int i = 0; i < magnitude.Length && carry != 0; i++)
        {
            ulong sum = magnitude[i] + carry;
            magnitude[i] = (uint)sum;
            carry = sum >> 32;
        }

        return FromMagnitude(magnitude, signBit: 1);
    }

    private static int CompareMagnitude(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right)
    {
        left = TrimSpan(left);
        right = TrimSpan(right);

        if (left.Length != right.Length)
        {
            return left.Length > right.Length ? 1 : -1;
        }

        for (int i = left.Length - 1; i >= 0; i--)
        {
            if (left[i] == right[i])
            {
                continue;
            }

            return left[i] > right[i] ? 1 : -1;
        }

        return 0;
    }

    private static uint[] AddMagnitude(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right)
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
        return TrimMagnitude(result);
    }

    private static uint[] AddMagnitudeUInt(ReadOnlySpan<uint> value, uint addend)
    {
        uint[] result = value.ToArray();
        if (result.Length == 0)
        {
            result = [0];
        }

        ulong carry = addend;
        int index = 0;

        while (carry != 0 && index < result.Length)
        {
            ulong sum = result[index] + carry;
            result[index] = (uint)sum;
            carry = sum >> 32;
            index++;
        }

        if (carry != 0)
        {
            Array.Resize(ref result, result.Length + 1);
            result[^1] = (uint)carry;
        }

        return TrimMagnitude(result);
    }

    private static uint[] SubtractMagnitude(ReadOnlySpan<uint> left, ReadOnlySpan<uint> right)
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

        return TrimMagnitude(result);
    }

    private static uint[] MultiplyMagnitudeByUInt(ReadOnlySpan<uint> value, uint multiplier)
    {
        if (multiplier == 0 || IsZeroMagnitude(value))
        {
            return [0];
        }

        uint[] result = new uint[value.Length + 1];
        ulong carry = 0;

        for (int i = 0; i < value.Length; i++)
        {
            ulong product = (ulong)value[i] * multiplier + carry;
            result[i] = (uint)product;
            carry = product >> 32;
        }

        result[value.Length] = (uint)carry;
        return TrimMagnitude(result);
    }

    private static uint[] DivideMagnitudeByUInt(uint[] value, uint divisor, out uint remainder)
    {
        if (divisor == 0)
        {
            throw new DivideByZeroException();
        }

        ulong rem = 0;
        uint[] quotient = new uint[value.Length];

        for (int i = value.Length - 1; i >= 0; i--)
        {
            ulong current = (rem << 32) | value[i];
            quotient[i] = (uint)(current / divisor);
            rem = current % divisor;
        }

        remainder = (uint)rem;
        return TrimMagnitude(quotient);
    }

    private static uint[] ShiftLeftMagnitude(ReadOnlySpan<uint> value, int shift)
    {
        if (shift == 0 || IsZeroMagnitude(value))
        {
            return value.ToArray();
        }

        int wordShift = shift / 32;
        int bitShift = shift % 32;
        uint[] result = new uint[value.Length + wordShift + 1];
        ulong carry = 0;

        for (int i = 0; i < value.Length; i++)
        {
            ulong shifted = ((ulong)value[i] << bitShift) | carry;
            result[i + wordShift] = (uint)shifted;
            carry = shifted >> 32;
        }

        if (carry != 0)
        {
            result[value.Length + wordShift] = (uint)carry;
        }

        return TrimMagnitude(result);
    }

    private static uint[] ShiftRightMagnitude(ReadOnlySpan<uint> value, int shift, out bool hasRemainder)
    {
        hasRemainder = false;

        if (shift == 0)
        {
            return value.ToArray();
        }

        int wordShift = shift / 32;
        int bitShift = shift % 32;

        if (wordShift >= value.Length)
        {
            hasRemainder = !IsZeroMagnitude(value);
            return [0];
        }

        for (int i = 0; i < wordShift; i++)
        {
            if (value[i] != 0)
            {
                hasRemainder = true;
                break;
            }
        }

        int resultLength = value.Length - wordShift;
        uint[] result = new uint[resultLength];

        if (bitShift == 0)
        {
            for (int i = 0; i < resultLength; i++)
            {
                result[i] = value[i + wordShift];
            }

            return TrimMagnitude(result);
        }

        uint remainderMask = (1u << bitShift) - 1;
        if ((value[wordShift] & remainderMask) != 0)
        {
            hasRemainder = true;
        }

        uint carry = 0;
        for (int i = value.Length - 1; i >= wordShift; i--)
        {
            uint current = value[i];
            result[i - wordShift] = (current >> bitShift) | carry;
            carry = current << (32 - bitShift);
        }

        return TrimMagnitude(result);
    }

    private static uint[] TrimMagnitude(uint[] digits)
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

    private static ReadOnlySpan<uint> TrimSpan(ReadOnlySpan<uint> digits)
    {
        int length = digits.Length;
        while (length > 0 && digits[length - 1] == 0)
        {
            length--;
        }

        return digits[..length];
    }

    private static bool IsZeroMagnitude(ReadOnlySpan<uint> digits)
    {
        digits = TrimSpan(digits);
        return digits.Length == 0 || digits.Length == 1 && digits[0] == 0;
    }

    private static int GetBitLength(ReadOnlySpan<uint> value)
    {
        value = TrimSpan(value);
        if (value.Length == 0)
        {
            return 0;
        }

        uint top = value[^1];
        int bits = (value.Length - 1) * 32;

        while (top != 0)
        {
            bits++;
            top >>= 1;
        }

        return bits;
    }

    private static bool GetBit(ReadOnlySpan<uint> value, int bit)
    {
        int word = bit / 32;
        int offset = bit % 32;
        return word < value.Length && ((value[word] >> offset) & 1) != 0;
    }

    private static void SetBit(uint[] value, int bit)
    {
        int word = bit / 32;
        int offset = bit % 32;
        value[word] |= 1u << offset;
    }

    private static int GetDigitValue(char c)
    {
        if (c >= '0' && c <= '9')
        {
            return c - '0';
        }

        if (c >= 'A' && c <= 'Z')
        {
            return c - 'A' + 10;
        }

        if (c >= 'a' && c <= 'z')
        {
            return c - 'a' + 10;
        }

        return -1;
    }

    private static void ValidateRadix(int radix)
    {
        if (radix < MinRadix || radix > MaxRadix)
        {
            throw new ArgumentOutOfRangeException(nameof(radix), $"Radix must be between {MinRadix} and {MaxRadix}.");
        }
    }

    private static void Reverse(StringBuilder builder)
    {
        for (int i = 0, j = builder.Length - 1; i < j; i++, j--)
        {
            (builder[i], builder[j]) = (builder[j], builder[i]);
        }
    }
}
