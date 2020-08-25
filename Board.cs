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
        public string EnPassant { get; private set; } = "";

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
            /*                                                     0   - ignore
             "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1"
              1                                           1  1   1   1 - parse
             we will ignore next parts: castle kingside and castle queenside information, passant target square, number of halfmoves */
            string[] parts = fen.Split();// split by ' '
            if (parts.Length != 6) return;

            InitFigures(parts[0]);
            currentPlayerColor = parts[1] == "b" ? Color.black : Color.white;
             
            ParseKingCastling(parts[2]);
            UpdateCastling();
            
            ParseEnPassant(parts[3]);

            moveNumber = int.Parse(parts[5]);
        }

        void ParseEnPassant(string text)
        {
            EnPassant = text;
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
                  " " + EnPassant + " " +
                  "0" + " " + // ignored information
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

            nextBoard.MakeEnPassantAttack(fm);
            nextBoard.MakeCastling(fm); 

            if (currentPlayerColor == Color.black)
                nextBoard.moveNumber++;

            nextBoard.EnPassant = nextBoard.UpdateEnPassant(fm);
            nextBoard.UpdateCastlingAfterMove(fm);
            nextBoard.UpdateCastling();

            nextBoard.currentPlayerColor = currentPlayerColor.FlipPlayer();// end turn
            
            nextBoard.GenerateFen();//apply the turn to current board state
            return nextBoard;
        }

        void MakeEnPassantAttack(FigureMoving fm)
        {
            Square midSquare = new Square(EnPassant);
            
            if (fm.to == midSquare)// en passant
            {
                InsertFigure(new Square(midSquare.x, fm.from.y), Figure.none);
            }
        }

        string UpdateEnPassant(FigureMoving fm)
        {
            if (fm.figure == Figure.whitePawn || fm.figure == Figure.blackPawn)
            {
                if (fm.AbsDeltaY == 2)
                {
                    int y = fm.to.y - fm.SignDeltaY;
                    Square midSquare = new Square(fm.to.x, y);
                    return midSquare.ToString();
                }
            }
            return "-";// en passant only has power during one move
        }

        void MakeCastling(FigureMoving fm)
        {
            if (fm.castling == 'K' || fm.castling == 'k')
            {
                InsertFigure(new Square(7, fm.from.y), Figure.none);
                InsertFigure(new Square(fm.to.x + (fm.SignDeltaX * -1), fm.to.y), fm.castling == 'K' ? Figure.whiteRook : Figure.blackRook);
            }
            else if (fm.castling == 'Q' || fm.castling == 'q')
            {
                InsertFigure(new Square(0, fm.from.y), Figure.none);
                InsertFigure(new Square(fm.to.x + (fm.SignDeltaX * -1), fm.to.y), fm.castling == 'Q' ? Figure.whiteRook : Figure.blackRook);
            }
        }

        void UpdateCastlingAfterMove(FigureMoving fm)
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
        }
        void UpdateCastling()
        {
            if (FigureAt(new Square(4, 0)) != Figure.whiteKing)
            {
                DisableKingsideCastling(Color.white);
                DisableQueensideCastling(Color.white);
            }

            if (FigureAt(new Square(4, 7)) != Figure.blackKing)
            {
                DisableKingsideCastling(Color.black);
                DisableQueensideCastling(Color.black);
            }

            if (FigureAt(new Square(0, 0)) != Figure.whiteRook)
            {
                DisableQueensideCastling(Color.white);
            }
            if (FigureAt(new Square(7, 0)) != Figure.whiteRook)
            {
                DisableKingsideCastling(Color.white);
            }

            if (FigureAt(new Square(0, 7)) != Figure.blackRook)
            {
                DisableQueensideCastling(Color.black);
            }
            if (FigureAt(new Square(7, 7)) != Figure.blackRook)
            {
                DisableKingsideCastling(Color.black);
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
