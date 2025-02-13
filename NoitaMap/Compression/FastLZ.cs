﻿using System.Runtime.InteropServices;
using CommunityToolkit.HighPerformance;

namespace NoitaMap.Compression;

public partial class FastLZ
{
    static FastLZ()
    {
        NativeLibrary.SetDllImportResolver(typeof(FastLZ).Assembly, (dllName, assembly, importSearchPath) =>
        {
            if (dllName == "fastlz")
            {
                string libraryPath = Path.Combine(File.Exists(assembly.Location) ? Path.GetDirectoryName(assembly.Location)! : Environment.CurrentDirectory, "Assets", "Libraries");

                if (Environment.Is64BitProcess)
                {
                    return NativeLibrary.Load(Path.Combine(libraryPath, "x64", "fastlz.dll"));
                }
                else
                {
                    return NativeLibrary.Load(Path.Combine(libraryPath, "x86", "fastlz.dll"));
                }
            }

            return 0;
        });
    }

    /// <summary>
    /// Decompresses a <![CDATA[Span<byte>]]>
    /// </summary>
    /// <param name="input">Bytes to decompress</param>
    /// <param name="output">Output buffer for decompressed bytes</param>
    /// <returns>Number of bytes decompressed</returns>
    public static int Decompress(Span<byte> input, Span<byte> output)
    {
        return fastlz_decompress(ref input.DangerousGetReference(), input.Length, ref output.DangerousGetReference(), output.Length);
    }

    public static Span<byte> Compress(int level, Span<byte> input)
    {
        // +10% just in case
        Span<byte> outputBuffer = new byte[input.Length + (int)((float)input.Length * 0.1f)];

        int length = fastlz_compress_level(level, ref input.DangerousGetReference(), input.Length, ref outputBuffer.DangerousGetReference());

        return outputBuffer[..length];
    }

    [LibraryImport("fastlz")]
    private static partial int fastlz_decompress(ref byte input, int inputLength, ref byte output, int outputLength);

    [LibraryImport("fastlz")]
    private static partial int fastlz_compress_level(int level, ref byte input, int inputLength, ref byte output);
}
