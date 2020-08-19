using System;
using System.Collections.Generic;
using System.Text;

namespace Chess
{
    class FigureMoving
    {
        public Figure figure { get; private set; }
        public Square from { get; private set; }
        public Square to { get; private set; }
        public Figure promotion { get; private set; }

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
            this.figure = (Figure)move[0];
            this.from = new Square(move.Substring(1, 2));
            this.to = new Square(move.Substring(3, 2));
            this.promotion = (move.Length == 6) ? (Figure)move[5] : Figure.none;
        }
    }
}
