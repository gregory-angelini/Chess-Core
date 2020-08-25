﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace ChessCore
{
    struct Square
    {
        public static Square none = new Square(-1, -1);
        public int x { get; private set; }
        public int y { get; private set; }

        public Square(int x, int y)
        {
            this.x = x;
            this.y = y;
        }


        // x: a, b, c, d, e, f, g, h
        // y: 1, 2, 3, 4, 5, 6, 7, 8
        // example: a4
        public Square(string name)
        {
            if(name.Length == 2 &&
               name[0] >= 'a' && name[0] <= 'h' &&
               name[1] >= '1' && name[1] <= '8')
            {
                x = name[0] - 'a';
                y = name[1] - '1';
            }
            else
            {
                this = none;
            }
        }

        public bool OnBoard()
        {
            return x >= 0 && x < 8 &&
                   y >= 0 && y < 8;
        }

        public static bool operator == (Square a, Square b)
        {
            return a.x == b.x && a.y == b.y;
        }

        public static bool operator != (Square a, Square b)
        {
            return !(a == b);
        }

        public static IEnumerable<Square> YieldSquares()
        {
            for(int y = 0; y < 8; y++)
                for(int x = 0; x < 8; x++)
                    yield return new Square(x, y);
        }

        public static string GetName(int x, int y)
        { 
            return ((char)('a' + x)).ToString() + (y + 1).ToString();
        }

        public override string ToString()
        {
            return ((char)('a' + x)).ToString() + (y + 1).ToString();
        }
    }
}
