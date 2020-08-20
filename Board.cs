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

        void Init()
        {
            /*                                               0   0 0   - ignore
             "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1"
              1                                           1          1 - parse
             we will ignore next parts: castle kingside and castle queenside information, passant target square, number of halfmoves */
            string[] parts = fen.Split();// split by ' '
            if (parts.Length != 6) return;

            InitFigures(parts[0]);
            moveColor = parts[1] == "b" ? Color.black : Color.white;
            moveNumber = int.Parse(parts[5]);
        }

        void InitFigures(string data)
        {
            // make line size 8
            //8
            //71
            //611
            //5111
            //41111
            //311111
            //2111111
            //11111111
            for(int j = 8; j >= 2; j--)
            {
                data = data.Replace(j.ToString(), (j - 1).ToString() + "1");
            }
            data = data.Replace("1", ".");

            string[] lines = data.Split('/');


            for(int y = 0; y < 8; y++)
            {
                for(int x = 0; x < 8; x++)
                {
                    figures[x, 7 - y] = lines[y][x] == '.' ? Figure.none : (Figure)lines[y][x];
                }
            }
        }

        public Figure FigureAt(Square square)
        {
            if(square.OnBoard())
            {
                return figures[square.x, square.y];
            }
            return Figure.none;
        }

        void InsertFigure(Square square, Figure figure)
        {
            if (square.OnBoard())
            {
                figures[square.x, square.y] = figure;
            }
        }

        void GenerateFen()
        {
            fen = FenFigures() + " " +
                  (moveColor == Color.white ? "w" : "b") + " " +
                  "- - 0 " + // ignored information
                  moveNumber.ToString();
        }

        string FenFigures()
        {
            StringBuilder sb = new StringBuilder();
           
            for (int y = 7; y >= 0; y--)
            {
                for (int x = 0; x < 8; x++)
                {
                    sb.Append(figures[x, y] == Figure.none ? '1' : (char)figures[x, y]);
                }
                if(y > 0) sb.Append('/');
            }

            // convert back to FEN notation
            string eight = "11111111";
            for(int j = 8; j >= 2; j--)
            {
                sb.Replace(eight.Substring(0, j), j.ToString());
            }

            return sb.ToString();
        }

  
        public IEnumerable<FigureOnSquare> YieldFigures()
        {
            foreach (Square square in Square.YieldSquares())
            {
                Figure figure = FigureAt(square);
                if (figure.GetColor() == moveColor)
                {
                    yield return new FigureOnSquare(figure, square);
                }
            }
        }

        public Board Move(FigureMoving fm)
        {
            Board nextBoard = new Board(fen);
            nextBoard.InsertFigure(fm.from, Figure.none);
            nextBoard.InsertFigure(fm.to, fm.promotion == Figure.none ? fm.figure : fm.promotion);
            
            if (moveColor == Color.black)
                nextBoard.moveNumber++;

            nextBoard.moveColor = moveColor.FlipColor();// end turn
            nextBoard.GenerateFen();//apply the turn to current board state
            return nextBoard;
        }

        bool IsOurKingUnderAttack()
        {
            Square enemyKing = FindEnemyKing();
            Moves moves = new Moves(this);
            
            foreach(FigureOnSquare fs in YieldFigures())
            {
                FigureMoving fm = new FigureMoving(fs, enemyKing);
                if(moves.CanMove(fm))// if an enemy figure can attack our king
                {
                    return true;
                }
            }
            return false;
        }

        Square FindEnemyKing()
        {
            Figure enemyKing = moveColor == Color.black ? Figure.whiteKing : Figure.blackKing;
            
            foreach(Square square in Square.YieldSquares())
            {
                if(FigureAt(square) == enemyKing)
                {
                    return square;
                }
            }
            return Square.none;
        }

        public bool IsCheck()
        {
            Board after = new Board(fen);
            after.moveColor = moveColor.FlipColor();
            // now we can work with enemy figures
            return after.IsOurKingUnderAttack();
        }

        public bool IsCheckAfterMove(FigureMoving fm)
        {
            Board after = Move(fm);
            return after.IsOurKingUnderAttack();
        }
    }
}
