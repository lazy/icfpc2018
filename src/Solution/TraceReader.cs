﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Sample
{
    public class TraceReader
    {
        public static List<object> Read(byte[] bytes)
        {
            var result = new List<object>();
            int startIndex = 0;
            while (startIndex < bytes.Length)
            {
                int commandSize;
                var command = ReadOneCommand(bytes, startIndex, out commandSize);
                startIndex += commandSize;
                result.Add(command);
            }

            return result;
        }

        public static object ReadOneCommand(byte[] bytes, int startIndex, out int commandSize)
        {
            var firstByte = bytes[startIndex];
            if (firstByte == 0b1111_1111)
            {
                commandSize = 1;
                return new Halt();
            }

            if (firstByte == 0b1111_1110)
            {
                commandSize = 1;
                return new Wait();
            }

            if (firstByte == 0b1111_1101)
            {
                commandSize = 1;
                return new Flip();
            }

            var lastFourBits = firstByte & 0b0000_1111;
            byte SecondByte() => bytes[startIndex + 1];

            if (lastFourBits == 0b0000_0100)
            {
                var axis = (firstByte & 0b00_11_0000) >> 4;
                var delta = SecondByte() & 0b000_11111;
                commandSize = 2;
                return new StraightMove
                {
                    Diff = ReadDiff(axis, delta, 15),
                };
            }

            if (lastFourBits == 0b0000_1100)
            {
                var axis1 = (firstByte & 0b00_11_0000) >> 4;
                var delta1 = SecondByte() & 0b0000_1111;
                var axis2 = (firstByte & 0b11_00_0000) >> 6;
                var delta2 = (SecondByte() & 0b1111_0000) >> 4;
                commandSize = 2;
                return new LMove
                {
                    Diff1 = ReadDiff(axis1, delta1, 5),
                    Diff2 = ReadDiff(axis2, delta2, 5),
                };
            }

            throw new Exception(string.Format("Unknown command start with {0}", firstByte));
        }

        private static CoordDiff ReadDiff(int axis, int delta, int correction)
        {
            var shift = delta - correction;
            return new CoordDiff
            {
                Dx = GetShiftForAxis(X_AXIS, axis, shift),
                Dy = GetShiftForAxis(Y_AXIS, axis, shift),
                Dz = GetShiftForAxis(Z_AXIS, axis, shift),
            };
        }

        private static int GetShiftForAxis(int neededAxis, int axis, int shift)
        {
            return neededAxis == axis ? shift : 0;
        }

        private const int X_AXIS = 0b01;
        private const int Y_AXIS = 0b10;
        private const int Z_AXIS = 0b11;
    }
}