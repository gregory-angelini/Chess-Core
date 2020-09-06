using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Runtime.CompilerServices;
using System.Text;

namespace ChessCore
{
    class Board
    {
        public string fen { get; private set; }
        Figure[,] figures;// array of all figures
        public string enPassant { get; private set; } = "";

        public Color moveColor { get; private set; }// who has a turn?
        public int moveNumber { get; private set; }// increases after black did a first move
        public int drawNumber { get; private set; } // increases after we move a non-pawn figure or do not attack enemy figure; when we get 50 the game is draw
        public bool wKingsideCastling { get; set; } = false;
        public bool wQueensideCastling { get; set; } = false;
        public bool bKingsideCastling { get; set; } = false;
        public bool bQueensideCastling { get; set; } = false;


        public Board(string fen)
        {
            this.fen = fen;
            figures = new Figure[8, 8];
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
            InitColor(parts[1]);
            InitCastling(parts[2]);
            //UpdateCastling(); 
            
            InitEnPassant(parts[3]);
            InitDrawNumber(parts[4]);
            InitMoveNumber(parts[5]);
        }

        void InitDrawNumber(string number)
        {
            drawNumber = int.Parse(number);
        }

        void InitMoveNumber(string text)
        {
            moveNumber = int.Parse(text);
        }

        void InitColor(string text)
        {
            moveColor = text == "b" ? Color.black : Color.white;
        }

        void InitEnPassant(string text)
        {
            enPassant = text;
        }

        void InitCastling(string text)
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
            data = data.Replace('1', (char)Figure.none);

            string[] lines = data.Split('/');


            for(int y = 0; y < 8; y++)
            {
                for(int x = 0; x < 8; x++)
                {
                    figures[x, 7 - y] = (Figure)lines[y][x];
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
                  FenMoveColor() + " " +
                  FenCastling() + " " + 
                  FenEnPassant() + " " +
                  FenDrawNumber() + " " +
                  FenMoveNumber();
        }

        string FenMoveNumber()
        {
            return moveNumber.ToString();
        }

        string FenDrawNumber()
        {
            return drawNumber.ToString();
        }

        string FenEnPassant()
        {
            return enPassant;
        }

        string FenCastling()
        {
            string flags = 
            (wKingsideCastling ? "K" : "") +
            (wQueensideCastling ? "Q" : "") +
            (bKingsideCastling ? "k" : "") +
            (bQueensideCastling ? "q" : "");

            return flags == "" ? "-" : flags;
        }

        string FenMoveColor()
        {
            return moveColor == Color.white ? "w" : "b";
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

                if(y > 0) 
                    sb.Append('/');
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

            nextBoard.MakeEnPassantAttack(fm);
            nextBoard.MakeCastling(fm); 

            if (moveColor == Color.black)
                nextBoard.moveNumber++;

            nextBoard.UpdateEnPassant(fm);
            nextBoard.UpdateCastlingAfterMove(fm);
            //nextBoard.UpdateCastling(); 

            nextBoard.moveColor = moveColor.FlipColor();// end of the move

            nextBoard.GenerateFen();//apply the move to current board state
            return nextBoard;
        }

        void MakeEnPassantAttack(FigureMoving fm)
        {
            Square midSquare = new Square(enPassant);
            
            if (fm.to == midSquare)// en passant
            {
                InsertFigure(new Square(midSquare.x, fm.from.y), Figure.none);
            }
        }

        void UpdateEnPassant(FigureMoving fm)
        {
            if (fm.figure == Figure.whitePawn || fm.figure == Figure.blackPawn)
            {
                if (fm.AbsDeltaY == 2)
                {
                    int y = fm.to.y - fm.SignDeltaY;
                    Square midSquare = new Square(fm.to.x, y);
                    enPassant = midSquare.ToString();
                }
            }
            enPassant = "-";// en passant only has power during one move
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
            Figure king = moveColor == Color.white ? Figure.whiteKing : Figure.blackKing;
            Figure rook = moveColor == Color.white ? Figure.whiteRook : Figure.blackRook;

            if (fm.figure == king)
            {
                DisableQueensideCastling(moveColor);
                DisableKingsideCastling(moveColor);
            }
            else if(fm.figure == rook)
            {
                if(fm.from.x == 0)
                {
                    DisableQueensideCastling(moveColor);
                } 
                else if(fm.from.x == 7)
                {
                    DisableKingsideCastling(moveColor);
                }
            }
        }
        
        /*
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
        }*/
            
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

        public bool IsEnemyKingUnderAttack()
        {
            Square enemyKing = FindEnemyKing();
            Moves moves = new Moves(this); 
            
            foreach(FigureOnSquare fs in YieldFigures())
            {
                FigureMoving fm = new FigureMoving(fs, enemyKing);
                fm.castling = '-';// ban castling
                if (moves.CanMove(fm))// if any of our figure can attack the enemy king
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

        public bool IsOurKingInCheck()
        {
            Board afterBoard = new Board(fen);
            afterBoard.moveColor = moveColor.FlipColor();
            // now we can work with enemy figures
            return afterBoard.IsEnemyKingUnderAttack();
        }
    }
}
