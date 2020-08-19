using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;

namespace Chess
{
    class Board
    {
        public string fen { get; private set; }
        Figure[,] figures;// array of all figures
        public Color moveColor { get; private set; }// who has a turn?
        public int moveNumber { get; private set; }// increases after black had a first turn

        public Board(string fen)
        {
            this.fen = fen;
            this.figures = new Figure[8, 8];
            Init();
        }

        // test
        void Init()
        {
            Insert(new Square("a1"), Figure.whiteKing);
            Insert(new Square("h8"), Figure.blackKing);
        }

        public Figure FigureAt(Square square)
        {
            if(square.OnBoard())
            {
                return figures[square.x, square.y];
            }
            return Figure.none;
        }

        void Insert(Square square, Figure figure)
        {
            if (square.OnBoard())
            {
                figures[square.x, square.y] = figure;
            }
        }


        public Board Move(FigureMoving fm)
        {
            Board nextBoard = new Board(fen);
            nextBoard.Insert(fm.from, Figure.none);
            nextBoard.Insert(fm.to, fm.promotion == Figure.none ? fm.figure : fm.promotion);
            
            if (moveColor == Color.black)
                nextBoard.moveNumber++;

            nextBoard.moveColor.FlipColor();// end turn
            return nextBoard;
        }
    }
}
