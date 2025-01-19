using System;
using System.Collections.Generic;
using System.Numerics;

namespace Note_Recognizer
{
   public class DSPEngine
   {
      public int WindowSize { get; set; }
      public int StepSize { get; set; }
      public int SampleRate { get; set; }
      public float Tolerance { get; set; }
      public float[] Data { get; set; }
      public List<String> Keys { get; set; }
      public List<float> KeyFreqs { get; set; }
      public List<float> Freqs { get; set; }

      public Int16 LoadProgress { get; set; }

      private static String[] keys = { "C", "Db", "D", "Eb", "E", "F", "Gb", "G", "Ab", "A", "Bb", "B" };

      private static float[] freqs =
      {
         65.4064f, 69.2957f, 73.4162f, 77.7817f, 82.4069f, 87.3071f, 92.4986f, 97.9989f, 103.826f, 110.000f, 116.541f, 123.471f, // C2
         130.813f, 138.591f, 146.832f, 155.563f, 164.814f, 174.614f, 184.997f, 195.998f, 207.652f, 220.000f, 233.082f, 246.942f, // C3
         261.626f, 277.183f, 293.665f, 311.127f, 329.628f, 349.228f, 369.994f, 391.995f, 415.305f, 440.000f, 466.164f, 493.883f, // C4
         523.251f, 554.365f, 587.330f, 622.254f, 659.255f, 698.456f, 739.989f, 783.991f, 830.609f, 880.000f, 932.328f, 987.767f, // C5
         1046.50f, 1108.73f, 1174.66f, 1244.51f, 1318.51f, 1396.91f, 1479.98f, 1567.98f, 1661.22f, 1760.00f, 1864.66f, 1975.53f, // C6
         2093.00f, 2217.46f, 2349.32f, 2489.02f, 2637.02f, 2793.83f, 2959.96f, 3135.96f, 3322.44f, 3520.00f, 3729.31f, 3951.07f, // C7
         4186.01f // C8
      };

      public DSPEngine()
      {
         Tolerance = 10;
      }

      public void Start()
      {
         float[] hannWindow = CreateHanningWindow();
         Keys = new List<string>(Data.Length / WindowSize + 1); // it will increase when windows are overlaping
         KeyFreqs = new List<float>(Data.Length / WindowSize + 1); // it will increase when windows are overlaping
         Freqs = new List<float>(Data.Length / WindowSize + 1); // it will increase when windows are overlaping

         for (int i = 0; i < Data.Length - WindowSize; i += StepSize)
         {
            Complex[] data = WindowMulData(hannWindow, i);
            Complex[] longfft = FFT(data);
            Complex[] fft = new Complex[longfft.Length / 2 + 1];
            for (int j = 0; j < fft.Length; j++)
            {
               fft[j] = Math.Abs(longfft[j].Real);
            }

            Complex[] hps1 = HarmonicProductSpectrum(fft);
            int maxBean1 = FindMaxBeanPosition(hps1);
            float baseFreq1 = ConvertToFreq(maxBean1);
            float keyFreq = baseFreq1;
            String key = GetKey(baseFreq1, Tolerance, out keyFreq);

            Freqs.Add(baseFreq1);
            KeyFreqs.Add(keyFreq);
            Keys.Add(key);
            LoadProgress = Convert.ToInt16(Scaler.ScaleToLong(i, Data.Length, 0, 100, 0));
         }
      }

      public void StartSingleWindow()
      {
         WindowSize = Data.Length;
         StepSize = Data.Length;

         float[] hannWindow = CreateHanningWindow();
         Keys = new List<string>(2);
         KeyFreqs = new List<float>(2);
         Freqs = new List<float>(2);

         Complex[] data = WindowMulData(hannWindow, 0);
         Complex[] longfft = FFT(data);
         Complex[] fft = new Complex[longfft.Length / 2 + 1];
         for (int j = 0; j < fft.Length; j++)
         {
            fft[j] = Math.Abs(longfft[j].Real);
         }

         Complex[] hps1 = HarmonicProductSpectrum(fft);
         int maxBean1 = FindMaxBeanPosition(hps1);
         float baseFreq1 = ConvertToFreq(maxBean1);
         float keyFreq = baseFreq1;
         String key = GetKey(baseFreq1, Tolerance, out keyFreq);

         Freqs.Add(baseFreq1);
         KeyFreqs.Add(keyFreq);
         Keys.Add(key);
         LoadProgress = Convert.ToInt16(Scaler.ScaleToLong(0, Data.Length, 0, 100, 0));
      }

      public Complex[] WindowMulData(float[] window, int dataStartPos)
      {
         Complex[] array = new Complex[window.Length];
         for (int i = 0; i < window.Length; i++)
         {
            array[i] = window[i] * Data[i + dataStartPos];
         }

         return array;
      }

      public Complex[] HarmonicProductSpectrum(Complex[] data)
      {
         Complex[] hps2 = Downsample(data, 2);
         Complex[] hps3 = Downsample(data, 3);
         Complex[] hps4 = Downsample(data, 4);
         Complex[] hps5 = Downsample(data, 5);
         Complex[] array = new Complex[hps5.Length];
         for (int i = 0; i < array.Length; i++)
         {
            checked
            {
               array[i] = data[i].Real * hps2[i].Real * hps3[i].Real * hps4[i].Real * hps5[i].Real;
            }
         }

         return array;
      }

      public Complex[] Downsample(Complex[] data, int n)
      {
         Complex[] array = new Complex[Convert.ToInt32(Math.Ceiling(data.Length * 1.0 / n))];
         for (int i = 0; i < array.Length; i++)
         {
            array[i] = data[i * n].Real;
         }

         return array;
      }

      public Complex[] DFT(Complex[] data)
      {
         int len = data.Length;
         int n = len / 2 + 1; // Maybe we should use len / 2 + 1 -> only so called "positive" frequencies, they are symetric anyway
         Complex[] realPart = new Complex[n];
         float pi_div = -2.0f * (float)MathF.PI / (float)len;

         for (int i = 0; i < n; i++)
         {
            for (int j = 0; j < len; j++)
            {
               realPart[i] += data[j].Real * Math.Cos(pi_div * (float)i * (float)j);
            }
         }

         for (int i = 0; i < n; i++)
         {
            realPart[i] = Math.Abs(realPart[i].Real);
         }

         return realPart;
      }

      public float[] CreateHanningWindow()
      {
         return CreateHanningWindow(WindowSize);
      }

      public float[] CreateHanningWindow(int size)
      {
         float[] array = new float[size];
         for (int i = 0; i < size; i++)
         {
            array[i] = 0.5f * (1 - MathF.Cos(2 * MathF.PI * i / size));
         }

         return array;
      }

      public int FindMaxBeanPosition(Complex[] data)
      {
         float max = float.MinValue;
         int pos = -1;
         // skip 0 freq
         for (int i = 1; i < data.Length; i++)
         {
            if (data[i].Real > max)
            {
               max = (float)data[i].Real;
               pos = i;
            }
         }

         return pos;
      }

      public float ConvertToFreq(int pos)
      {
         return pos * 1.0f / WindowSize * SampleRate;
      }

      public static Complex[] FFT(Complex[] x)
      {
         int N = x.Length;

         // base case
         if (N == 1) return new Complex[] { x[0] };

         // radix 2 Cooley-Tukey FFT
         if (N % 2 != 0)
         {
            throw new Exception("N is not a power of 2");
         }

         // fft of even terms
         Complex[] even = new Complex[N / 2];
         for (int k = 0; k < N / 2; k++)
         {
            even[k] = x[2 * k];
         }

         Complex[] q = FFT(even);

         // fft of odd terms
         Complex[] odd = even; // reuse the array
         for (int k = 0; k < N / 2; k++)
         {
            odd[k] = x[2 * k + 1];
         }

         Complex[] r = FFT(odd);

         // combine
         Complex[] y = new Complex[N];
         for (int k = 0; k < N / 2; k++)
         {
            float kth = -2.0f * (float)k * (float)Math.PI / (float)N;
            Complex wk = new Complex(Math.Cos(kth), Math.Sin(kth));
            y[k] = q[k] + (wk * (r[k]));
            y[k + N / 2] = q[k] - (wk * (r[k]));
         }

         return y;
      }

      public static void transformRadix2(float[] real, float[] imag)
      {
         // Initialization
         if (real.Length != imag.Length)
            throw new Exception("Mismatched lengths");
         int n = real.Length;

         int levels = 31 - (int)Math.Log(n, 2); // Equal to floor(log2(n))
         if (1 << levels != n)
            throw new Exception("Length is not a power of 2");
         float[] cosTable = new float[n / 2];
         float[] sinTable = new float[n / 2];
         for (int i = 0; i < n / 2; i++)
         {
            cosTable[i] = MathF.Cos(2.0f * MathF.PI * i / n);
            sinTable[i] = MathF.Sin(2.0f * MathF.PI * i / n);
         }

         // Bit-reversed addressing permutation
         for (uint i = 0; i < n; i++)
         {
            //uint j = Integer.reverse(i) >> (32 - levels);
            uint j = reverseBits(i, 32 - levels);
            if (j > i)
            {
               float temp = real[i];
               real[i] = real[j];
               real[j] = temp;
               temp = imag[i];
               imag[i] = imag[j];
               imag[j] = temp;
            }
         }

         // Cooley-Tukey decimation-in-time radix-2 FFT
         for (int size = 2; size <= n; size *= 2)
         {
            int halfsize = size / 2;
            int tablestep = n / size;
            for (int i = 0; i < n; i += size)
            {
               for (int j = i, k = 0; j < i + halfsize; j++, k += tablestep)
               {
                  float tpre = real[j + halfsize] * cosTable[k] + imag[j + halfsize] * sinTable[k];
                  float tpim = -real[j + halfsize] * sinTable[k] + imag[j + halfsize] * cosTable[k];
                  real[j + halfsize] = real[j] - tpre;
                  imag[j + halfsize] = imag[j] - tpim;
                  real[j] += tpre;
                  imag[j] += tpim;
               }
            }

            if (size == n) // Prevent overflow in 'size *= 2'
               break;
         }
      }

      private static uint reverseBits(uint x, int n)
      {
         uint result = 0;
         uint i;
         for (i = 0; i < n; i++, x >>= 1)
            result = (result << 1) | (x & 1);
         return result;
      }

      public static String GetKey(float frequency, float tolerance, out float keyFreq)
      {
         String note = "";
         keyFreq = 0f;

         for (int i = 0; i < freqs.Length; i++)
         {
            if (Math.Abs(freqs[i] - frequency) <= tolerance)
            {
               int octave = i / 12 + 2;
               note = String.Format("{0}{1}", keys[i % 12], octave, frequency);
               keyFreq = freqs[i];
            }
         }

         return note;
      }
   }

   public class Scaler
   {
      public static float ScaleTofloat(long oldValue, long oldMax, long oldMin, long newMax, long newMin)
      {
         float oldRange = (oldMax - oldMin);
         float newRange = (newMax - newMin);
         return (((oldValue - oldMin) * newRange) / oldRange) + newMin;
      }

      public static long ScaleToLong(long oldValue, long oldMax, long oldMin, long newMax, long newMin)
      {
         checked
         {
            long oldRange = (oldMax - oldMin);
            long newRange = (newMax - newMin);
            return (((oldValue - oldMin) * newRange) / oldRange) + newMin;
         }
      }

      public static float ScaleTofloatGraphVersion(float oldValue, float oldMax, float oldMin, long newMax, long newMin)
      {
         float oldRange = (oldMax - oldMin);
         float newRange = (newMax - newMin);
         return (((oldValue - oldMin) * newRange) / oldRange) + newMin;
      }
   }
}