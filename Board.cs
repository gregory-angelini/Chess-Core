using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;

namespace ChessCore
{
    class Board
    {
        public string fen { get; private set; }
        Figure[,] figures;// array of all figures
        public Color currentPlayerColor { get; private set; }// who has a turn?
        public int moveNumber { get; private set; }// increases after black had a first turn

        public bool wKingsideCastling { get; set; } = false;
        public bool wQueensideCastling { get; set; } = false;
        public bool bKingsideCastling { get; set; } = false;
        public bool bQueensideCastling { get; set; } = false;


        public Board(string fen)
        {
            this.fen = fen;
            this.figures = new Figure[8, 8];
            Init();
        }

        void Init()
        {
            /*                                                   0 0   - ignore
             "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1"
              1                                           1  1       1 - parse
             we will ignore next parts: castle kingside and castle queenside information, passant target square, number of halfmoves */
            string[] parts = fen.Split();// split by ' '
            if (parts.Length != 6) return;

            InitFigures(parts[0]);
            currentPlayerColor = parts[1] == "b" ? Color.black : Color.white;

            ParseKingCastling(parts[2]);

            moveNumber = int.Parse(parts[5]);
        }

        void ParseKingCastling(string text)
        {
            foreach (char it in text)
            {
                switch (it)
                {
                    case 'K':
                        wKingsideCastling = true;
                        break;

                    case 'Q':
                        wQueensideCastling = true;
                        break;

                    case 'k':
                        bKingsideCastling = true;
                        break;

                    case 'q':
                        bQueensideCastling = true;
                        break;
                }
            }
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
                  (currentPlayerColor == Color.white ? "w" : "b") + " " +
                  (wKingsideCastling ? "K" : "") +
                  (wQueensideCastling ? "Q" : "") +
                  (bKingsideCastling ? "k" : "") +
                  (bQueensideCastling ? "q" : "") +
                  " - 0 " + // ignored information
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
                if (figure.GetColor() == currentPlayerColor)
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

            nextBoard.MakeCastling(nextBoard, fm);

            if (currentPlayerColor == Color.black)
                nextBoard.moveNumber++;

            nextBoard.UpdateCastling(fm);

            nextBoard.currentPlayerColor = currentPlayerColor.FlipPlayer();// end turn
            
            nextBoard.GenerateFen();//apply the turn to current board state
            return nextBoard;
        }

        void MakeCastling(Board board, FigureMoving fm)
        {
            if (fm.castling == 'K' || fm.castling == 'k')
            {
                board.InsertFigure(new Square(7, fm.from.y), Figure.none);
                board.InsertFigure(new Square(fm.to.x + (fm.SignDeltaX * -1), fm.to.y), fm.castling == 'K' ? Figure.whiteRook : Figure.blackRook);
            }
            else if (fm.castling == 'Q' || fm.castling == 'q')
            {
                board.InsertFigure(new Square(0, fm.from.y), Figure.none);
                board.InsertFigure(new Square(fm.to.x + (fm.SignDeltaX * -1), fm.to.y), fm.castling == 'Q' ? Figure.whiteRook : Figure.blackRook);
            }
        }

        void UpdateCastling(FigureMoving fm)
        {
            // we move our king or rook
            Figure king = currentPlayerColor == Color.white ? Figure.whiteKing : Figure.blackKing;
            Figure rook = currentPlayerColor == Color.white ? Figure.whiteRook : Figure.blackRook;

            if (fm.figure == king)
            {
                DisableQueensideCastling(currentPlayerColor);
                DisableKingsideCastling(currentPlayerColor);
            }
            else if(fm.figure == rook)
            {
                if(fm.from.x == 0)
                {
                    DisableQueensideCastling(currentPlayerColor);
                } 
                else if(fm.from.x == 7)
                {
                    DisableKingsideCastling(currentPlayerColor);
                }
            }

            // we kill an enemy rook
            if(FigureAt(fm.to) == Figure.blackRook)// currentPlayerColor == Color.white
            {
                if (fm.to.x == 0 && fm.to.y == 7)
                {
                    DisableQueensideCastling(Color.black);
                }
                else if (fm.to.x == 7 && fm.to.y == 7)
                {
                    DisableKingsideCastling(Color.black);
                }
            }
            else if(FigureAt(fm.to) == Figure.whiteRook)// currentPlayerColor == Color.black
            {
                if (fm.to.x == 0 && fm.to.y == 0)
                {
                    DisableQueensideCastling(Color.white);
                }
                else if (fm.to.x == 7 && fm.to.y == 0)
                {
                    DisableKingsideCastling(Color.white);
                }
            }
        }

        void DisableKingsideCastling(Color color)
        {
            if (color == Color.white)
            {
                wKingsideCastling = false;
            }
            else// if (color == Color.black)
            {
                bKingsideCastling = false;
            }
        }

        void DisableQueensideCastling(Color color)
        {
            if (color == Color.white)
            {
                wQueensideCastling = false;
            }
            else//if (color == Color.black)
            {
                bQueensideCastling = false;
            }
        }

        bool IsEnemyKingUnderAttack()
        {
            Figure king = currentPlayerColor == Color.white ? Figure.whiteKing : Figure.blackKing;
            Square enemyKing = FindEnemyKing();
            Moves moves = new Moves(this); 
            
            foreach(FigureOnSquare fs in YieldFigures())
            {
                if (fs.figure != king)// kings cannot attack each other
                {
                    FigureMoving fm = new FigureMoving(fs, enemyKing);
                    if (moves.CanMove(fm))// if any of our figure can attack enemy king
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        Square FindEnemyKing()
        {
            Figure enemyKing = currentPlayerColor == Color.black ? Figure.whiteKing : Figure.blackKing;
            
            foreach(Square square in Square.YieldSquares())
            {
                if(FigureAt(square) == enemyKing)
                {
                    return square;
                }
            }
            return Square.none;
        }

        public bool IsOurKingInCheck()
        {
            Board after = new Board(fen);
            after.currentPlayerColor = currentPlayerColor.FlipPlayer();
            // now we can work with enemy figures
            return after.IsEnemyKingUnderAttack();
        }

        public bool IsOurKingInCheckAfterMove(FigureMoving fm)
        {
            Board after = Move(fm);
            return after.IsEnemyKingUnderAttack();
        }
    }
}
