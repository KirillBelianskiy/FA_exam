using System.Diagnostics;
using System.IO;

namespace Arithmetic;

public static class MultiplierPerformanceDemo
{
    public static void Run(TextWriter output, int wordCount = 2048)
    {
        if (output == null)
        {
            throw new ArgumentNullException(nameof(output));
        }

        if (wordCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(wordCount));
        }

        uint[] left = CreateDigits(wordCount, 17);
        uint[] right = CreateDigits(wordCount, 29);

        Measure(output, "Simple", new SimpleMultiplier(), left, right);
        Measure(output, "Karatsuba", new KaratsubaMultiplier(), left, right);
        Measure(output, "FFT", new FftMultiplier(), left, right);
    }

    private static void Measure(TextWriter output, string name, IMultiplier multiplier, uint[] left, uint[] right)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        uint[] result = multiplier.Multiply(left, right);
        stopwatch.Stop();

        output.WriteLine($"{name}: {stopwatch.ElapsedMilliseconds} ms, result words: {result.Length}");
    }

    private static uint[] CreateDigits(int count, int seed)
    {
        Random random = new Random(seed);
        uint[] digits = new uint[count];

        for (int i = 0; i < digits.Length; i++)
        {
            uint high = (uint)random.Next(0, 65536);
            uint low = (uint)random.Next(0, 65536);
            digits[i] = (high << 16) | low;
            if (digits[i] == 0)
            {
                digits[i] = 1;
            }
        }

        digits[digits.Length - 1] |= 1;
        return digits;
    }
}
