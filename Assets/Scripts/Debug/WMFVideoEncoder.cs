#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN

using System;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Encodes a JPEG frame sequence to an H.264 MP4 file using Windows Media Foundation.
/// No external dependencies — WMF ships with every modern Windows install.
///
/// Must be called from the Unity main thread; JPEG decode uses UnityEngine.Texture2D.
/// Encoding is synchronous and will block for a few seconds on large recordings.
/// </summary>
public static class WMFVideoEncoder
{
    // ── Media type / attribute GUIDs ──────────────────────────────────────────

    static readonly Guid MFMediaType_Video        = new Guid("73646976-0000-0010-8000-00AA00389B71");
    static readonly Guid MFVideoFormat_H264        = new Guid("34363248-0000-0010-8000-00AA00389B71");
    static readonly Guid MFVideoFormat_RGB32       = new Guid("00000016-0000-0010-8000-00AA00389B71");
    static readonly Guid MF_MT_MAJOR_TYPE          = new Guid("48eba18e-f8c9-4687-bf11-0a74c9f96a8f");
    static readonly Guid MF_MT_SUBTYPE             = new Guid("f7e34c9a-42e8-4714-b74b-cb29d72c35e5");
    static readonly Guid MF_MT_FRAME_SIZE          = new Guid("1652c33d-d6b2-4012-b834-72030849a37d");
    static readonly Guid MF_MT_FRAME_RATE          = new Guid("c459a2e8-3d2c-4e44-b132-fee5156c7bb0");
    static readonly Guid MF_MT_AVG_BITRATE         = new Guid("20332624-fb0d-4d9e-bd0d-cbf6786c102e");
    static readonly Guid MF_MT_INTERLACE_MODE      = new Guid("e2724bb8-e676-4806-b4b2-a8d6efb44ccd");
    static readonly Guid MF_MT_PIXEL_ASPECT_RATIO  = new Guid("c6376a1e-8d0a-4027-be45-6d9a0ad39bb7");

    // ── P/Invoke ──────────────────────────────────────────────────────────────

    [DllImport("mfplat.dll",     ExactSpelling = true)] static extern int MFStartup(int version, int dwFlags = 0);
    [DllImport("mfplat.dll",     ExactSpelling = true)] static extern int MFShutdown();
    [DllImport("mfplat.dll",     ExactSpelling = true)] static extern int MFCreateMediaType(out IMFMediaType ppMFType);
    [DllImport("mfplat.dll",     ExactSpelling = true)] static extern int MFCreateMemoryBuffer(int cbMaxLength, out IMFMediaBuffer ppBuffer);
    [DllImport("mfplat.dll",     ExactSpelling = true)] static extern int MFCreateSample(out IMFSample ppIMFSample);

    [DllImport("mfreadwrite.dll", ExactSpelling = true)]
    static extern int MFCreateSinkWriterFromURL(
        [MarshalAs(UnmanagedType.LPWStr)] string pwszOutputURL,
        IntPtr pByteStream,   // null
        IntPtr pAttributes,   // null = default
        out IMFSinkWriter ppSinkWriter);

    // ── COM interfaces ────────────────────────────────────────────────────────
    // Every method must appear in exact vtable order.
    // Slots we never call are stubbed as void _N() — position is what matters.

    // IMFAttributes (30 methods)
    [ComImport, Guid("2CD2D921-C447-44A7-A13C-4ADABFC247E3"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMFAttributes
    {
        // 0-17 unused
        void _0();  void _1();  void _2();  void _3();  void _4();  void _5();  void _6();  void _7();
        void _8();  void _9();  void _10(); void _11(); void _12(); void _13(); void _14(); void _15();
        void _16(); void _17();
        // 18  SetUINT32
        void SetUINT32([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, int unValue);
        // 19  SetUINT64
        void SetUINT64([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, long unValue);
        // 20 unused
        void _20();
        // 21  SetGUID
        void SetGUID([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey,
                     [In, MarshalAs(UnmanagedType.LPStruct)] Guid guidValue);
        // 22-29 unused
        void _22(); void _23(); void _24(); void _25(); void _26(); void _27(); void _28(); void _29();
    }

    // IMFMediaType (inherits IMFAttributes vtable, then adds 5 own methods)
    // We flatten the vtable here — re-declare the 30 inherited slots, then 5 own.
    [ComImport, Guid("44AE0FA8-EA31-4109-8D2E-4CAE4997C555"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMFMediaType
    {
        // 0-17 unused (inherited)
        void _0();  void _1();  void _2();  void _3();  void _4();  void _5();  void _6();  void _7();
        void _8();  void _9();  void _10(); void _11(); void _12(); void _13(); void _14(); void _15();
        void _16(); void _17();
        // 18  SetUINT32
        void SetUINT32([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, int unValue);
        // 19  SetUINT64
        void SetUINT64([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey, long unValue);
        // 20 unused
        void _20();
        // 21  SetGUID
        void SetGUID([In, MarshalAs(UnmanagedType.LPStruct)] Guid guidKey,
                     [In, MarshalAs(UnmanagedType.LPStruct)] Guid guidValue);
        // 22-34 unused (inherited 22-29, own 30-34)
        void _22(); void _23(); void _24(); void _25(); void _26(); void _27(); void _28(); void _29();
        void _30(); void _31(); void _32(); void _33(); void _34();
    }

    // IMFMediaBuffer (5 methods, no base interface)
    [ComImport, Guid("045FA593-8799-42b8-BC8D-8968C6453507"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMFMediaBuffer
    {
        void Lock(out IntPtr ppbBuffer, out int pcbMaxLength, out int pcbCurrentLength); // 0
        void Unlock();                                                                    // 1
        void GetCurrentLength(out int pcbCurrentLength);                                 // 2
        void SetCurrentLength(int cbCurrentLength);                                      // 3
        void GetMaxLength(out int pcbMaxLength);                                         // 4
    }

    // IMFSample (inherits IMFAttributes vtable, then adds 14 own methods)
    [ComImport, Guid("C40A00F2-B93A-4D80-AE8C-5A1C634F58E4"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMFSample
    {
        // 0-29 unused (inherited IMFAttributes)
        void _0();  void _1();  void _2();  void _3();  void _4();  void _5();  void _6();  void _7();
        void _8();  void _9();  void _10(); void _11(); void _12(); void _13(); void _14(); void _15();
        void _16(); void _17(); void _18(); void _19(); void _20(); void _21(); void _22(); void _23();
        void _24(); void _25(); void _26(); void _27(); void _28(); void _29();
        // 30-32 unused
        void _30(); void _31(); void _32();
        // 33  SetSampleTime
        void SetSampleTime(long hnsSampleTime);
        // 34 unused (GetSampleDuration)
        void _34();
        // 35  SetSampleDuration
        void SetSampleDuration(long hnsSampleDuration);
        // 36-38 unused
        void _36(); void _37(); void _38();
        // 39  AddBuffer
        void AddBuffer(IMFMediaBuffer pBuffer);
        // 40-43 unused
        void _40(); void _41(); void _42(); void _43();
    }

    // IMFSinkWriter (11 methods, no base interface beyond IUnknown)
    [ComImport, Guid("3137F1CD-FE5E-4805-A5D8-FB477448CB3D"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMFSinkWriter
    {
        void AddStream(IMFMediaType pTargetMediaType, out int pdwStreamIndex);           // 0
        void SetInputMediaType(int dwStreamIndex, IMFMediaType pInputMediaType,
                               IMFAttributes pEncodingParameters);                       // 1
        void BeginWriting();                                                             // 2
        void WriteSample(int dwStreamIndex, IMFSample pSample);                         // 3
        void _4(); void _5(); void _6(); void _7();  // SendStreamTick, PlaceMark, NotifyEndOfSegment, Flush
        void FinalizeWriter();                        // 8  (COM method name: Finalize)
        void _9(); void _10();                       // GetServiceForStream, GetStatistics
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Encodes <paramref name="jpegFrames"/> to an H.264 MP4 file.
    /// Returns true on success. Logs the HRESULT on failure.
    /// </summary>
    public static bool Encode(byte[][] jpegFrames, int width, int height, int fps, string outputPath)
    {
        int hr = MFStartup(0x00020070 /* MF_VERSION */);
        if (hr < 0) { Debug.LogError($"[WMFVideoEncoder] MFStartup failed: 0x{hr:X8}"); return false; }

        IMFSinkWriter writer  = null;
        IMFMediaType  outType = null;
        IMFMediaType  inType  = null;
        Texture2D decodeTex   = null;

        try
        {
            // ── Create sink writer (writes directly to the output file) ─────────
            hr = MFCreateSinkWriterFromURL(outputPath, IntPtr.Zero, IntPtr.Zero, out writer);
            if (hr < 0) throw new Exception($"MFCreateSinkWriterFromURL 0x{hr:X8}");

            // ── Output stream: H.264 ─────────────────────────────────────────────
            MFCreateMediaType(out outType);
            outType.SetGUID(MF_MT_MAJOR_TYPE,         MFMediaType_Video);
            outType.SetGUID(MF_MT_SUBTYPE,            MFVideoFormat_H264);
            outType.SetUINT32(MF_MT_AVG_BITRATE,      4_000_000);           // 4 Mbps
            outType.SetUINT32(MF_MT_INTERLACE_MODE,   2);                   // Progressive
            outType.SetUINT64(MF_MT_FRAME_SIZE,        Pack(width, height));
            outType.SetUINT64(MF_MT_FRAME_RATE,        Pack(fps, 1));
            outType.SetUINT64(MF_MT_PIXEL_ASPECT_RATIO, Pack(1, 1));
            writer.AddStream(outType, out int streamIdx);
            Marshal.ReleaseComObject(outType); outType = null;

            // ── Input stream: RGB32 (BGRA byte order) ────────────────────────────
            // WMF automatically inserts a colour converter (RGB32 → YUV) before the encoder.
            MFCreateMediaType(out inType);
            inType.SetGUID(MF_MT_MAJOR_TYPE,          MFMediaType_Video);
            inType.SetGUID(MF_MT_SUBTYPE,             MFVideoFormat_RGB32);
            inType.SetUINT32(MF_MT_INTERLACE_MODE,    2);
            inType.SetUINT64(MF_MT_FRAME_SIZE,         Pack(width, height));
            inType.SetUINT64(MF_MT_FRAME_RATE,         Pack(fps, 1));
            inType.SetUINT64(MF_MT_PIXEL_ASPECT_RATIO, Pack(1, 1));
            writer.SetInputMediaType(streamIdx, inType, null);
            Marshal.ReleaseComObject(inType); inType = null;

            writer.BeginWriting();

            // ── Encode frames ────────────────────────────────────────────────────
            long frameDuration = 10_000_000L / fps;   // in 100-nanosecond units
            int  bufSize       = width * height * 4;
            var  bgra          = new byte[bufSize];
            decodeTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);

            for (int i = 0; i < jpegFrames.Length; i++)
            {
                // Decode JPEG → RGBA pixels (Unity stores rows bottom-up)
                decodeTex.LoadImage(jpegFrames[i]);
                Color32[] px = decodeTex.GetPixels32();

                // Convert Unity RGBA (bottom-up) → WMF RGB32 = BGRA (top-down)
                for (int row = 0; row < height; row++)
                {
                    int srcRow = height - 1 - row;          // flip vertical
                    for (int col = 0; col < width; col++)
                    {
                        Color32 c = px[srcRow * width + col];
                        int d = (row * width + col) * 4;
                        bgra[d]     = c.b;
                        bgra[d + 1] = c.g;
                        bgra[d + 2] = c.r;
                        bgra[d + 3] = 255;
                    }
                }

                // Fill WMF memory buffer
                MFCreateMemoryBuffer(bufSize, out IMFMediaBuffer mfBuf);
                mfBuf.Lock(out IntPtr bufPtr, out _, out _);
                Marshal.Copy(bgra, 0, bufPtr, bufSize);
                mfBuf.Unlock();
                mfBuf.SetCurrentLength(bufSize);

                // Wrap in a sample and submit
                MFCreateSample(out IMFSample sample);
                sample.AddBuffer(mfBuf);
                sample.SetSampleTime(i * frameDuration);
                sample.SetSampleDuration(frameDuration);
                writer.WriteSample(streamIdx, sample);

                Marshal.ReleaseComObject(sample);
                Marshal.ReleaseComObject(mfBuf);
            }

            writer.FinalizeWriter();
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[WMFVideoEncoder] Encoding failed: {ex.Message}");
            return false;
        }
        finally
        {
            if (decodeTex != null) UnityEngine.Object.Destroy(decodeTex);
            if (outType   != null) Marshal.ReleaseComObject(outType);
            if (inType    != null) Marshal.ReleaseComObject(inType);
            if (writer    != null) Marshal.ReleaseComObject(writer);
            MFShutdown();
        }
    }

    // Packs two 32-bit values into a single UINT64 attribute (WMF convention).
    static long Pack(int hi, int lo) => ((long)hi << 32) | (uint)lo;
}

#endif
