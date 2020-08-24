using System;
using System.Collections.Generic;
using System.Text;

namespace ChessCore
{
    class FigureMoving
    {
        public Figure figure { get; private set; }
        public Square from { get; private set; }
        public Square to { get; private set; }
        public Figure promotion { get; private set; }
        public char castling { get; set; } = ' ';

        public FigureMoving(FigureOnSquare fs, Square to, Figure promotion = Figure.none)
        {
            this.figure = fs.figure;
            this.from = fs.square;
            this.to = to;
            this.promotion = promotion;
        }

        // parses move from string to FigureMoving
        // move can be described with 4 or 5 symbols (promotion case)
        // example: Pe2e4, Pe7e8Q (promotion to Queen)
        public FigureMoving(string move)
        {
            figure = (Figure)move[0];
            from = new Square(move.Substring(1, 2));
            to = new Square(move.Substring(3, 2));
            promotion = (move.Length == 6) ? (Figure)move[5] : Figure.none;
        }

        public int DeltaX { get { return to.x - from.x; } }
        public int DeltaY { get { return to.y - from.y; } }

        public int AbsDeltaX { get { return Math.Abs(DeltaX); } }
        public int AbsDeltaY { get { return Math.Abs(DeltaY); } }

        public int SignDeltaX { get { return Math.Sign(DeltaX); } }
        public int SignDeltaY { get { return Math.Sign(DeltaY); } }

        public override string ToString()
        {
            string text = (char)figure + Square.GetName(from.x, from.y) + Square.GetName(to.x, to.y);

            if (promotion != Figure.none)
                text += (char)promotion;

            return text;
        }
    }
}
