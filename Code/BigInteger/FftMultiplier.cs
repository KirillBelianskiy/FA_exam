using System.Numerics;

namespace Arithmetic;

public sealed class FftMultiplier : IMultiplier
{
    private const int ChunkBits = 16;
    private const int ChunkBase = 1 << ChunkBits;
    private const double TwoPi = Math.PI * 2.0;

    public uint[] Multiply(uint[] left, uint[] right)
    {
        left = SimpleMultiplier.Trim(left);
        right = SimpleMultiplier.Trim(right);

        if (IsZero(left) || IsZero(right))
        {
            return [0];
        }

        double[] leftChunks = ToChunks(left);
        double[] rightChunks = ToChunks(right);
        int size = 1;
        while (size < leftChunks.Length + rightChunks.Length)
        {
            size <<= 1;
        }

        Complex[] fa = new Complex[size];
        Complex[] fb = new Complex[size];

        for (int i = 0; i < leftChunks.Length; i++)
        {
            fa[i] = new Complex(leftChunks[i], 0);
        }

        for (int i = 0; i < rightChunks.Length; i++)
        {
            fb[i] = new Complex(rightChunks[i], 0);
        }

        Fft(fa, invert: false);
        Fft(fb, invert: false);

        for (int i = 0; i < size; i++)
        {
            fa[i] *= fb[i];
        }

        Fft(fa, invert: true);

        ulong carry = 0;
        uint[] chunks = new uint[size + 1];

        for (int i = 0; i < size; i++)
        {
            ulong value = (ulong)Math.Round(fa[i].Real) + carry;
            chunks[i] = (uint)(value & (ChunkBase - 1));
            carry = value >> ChunkBits;
        }

        int carryIndex = size;
        while (carry != 0)
        {
            chunks[carryIndex++] = (uint)(carry & (ChunkBase - 1));
            carry >>= ChunkBits;
        }

        return PackChunks(chunks);
    }

    private static double[] ToChunks(uint[] digits)
    {
        double[] chunks = new double[digits.Length * 2];

        for (int i = 0; i < digits.Length; i++)
        {
            chunks[i * 2] = digits[i] & 0xFFFF;
            chunks[i * 2 + 1] = digits[i] >> 16;
        }

        return chunks;
    }

    private static uint[] PackChunks(uint[] chunks)
    {
        int digitCount = (chunks.Length + 1) / 2;
        uint[] result = new uint[digitCount];

        for (int i = 0; i < digitCount; i++)
        {
            uint low = i * 2 < chunks.Length ? chunks[i * 2] : 0;
            uint high = i * 2 + 1 < chunks.Length ? chunks[i * 2 + 1] : 0;
            result[i] = low | (high << 16);
        }

        return SimpleMultiplier.Trim(result);
    }

    private static void Fft(Complex[] values, bool invert)
    {
        int n = values.Length;

        for (int i = 1, j = 0; i < n; i++)
        {
            int bit = n >> 1;
            for (; (j & bit) != 0; bit >>= 1)
            {
                j ^= bit;
            }

            j ^= bit;

            if (i < j)
            {
                (values[i], values[j]) = (values[j], values[i]);
            }
        }

        for (int length = 2; length <= n; length <<= 1)
        {
            double angle = TwoPi / length * (invert ? -1 : 1);
            Complex root = new(Math.Cos(angle), Math.Sin(angle));

            for (int i = 0; i < n; i += length)
            {
                Complex current = Complex.One;
                int half = length >> 1;

                for (int j = 0; j < half; j++)
                {
                    Complex even = values[i + j];
                    Complex odd = values[i + j + half] * current;
                    values[i + j] = even + odd;
                    values[i + j + half] = even - odd;
                    current *= root;
                }
            }
        }

        if (invert)
        {
            for (int i = 0; i < n; i++)
            {
                values[i] /= n;
            }
        }
    }

    private static bool IsZero(uint[] digits)
    {
        return digits.Length == 0 || digits.Length == 1 && digits[0] == 0;
    }
}
