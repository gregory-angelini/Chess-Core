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
        public bool wKingsideCastle { get; set; } = false;
        public bool wQueensideCastle { get; set; } = false;
        public bool bKingsideCastle { get; set; } = false;
        public bool bQueensideCastle { get; set; } = false;


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
            InitCastle(parts[2]);
            
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

        void InitCastle(string text)
        {
            foreach (char it in text)
            {
                switch (it)
                {
                    case 'K':
                        wKingsideCastle = true;
                        break;

                    case 'Q':
                        wQueensideCastle = true;
                        break;

                    case 'k':
                        bKingsideCastle = true;
                        break;

                    case 'q':
                        bQueensideCastle = true;
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
                  FenCastle() + " " + 
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

        string FenCastle()
        {
            string flags = 
            (wKingsideCastle ? "K" : "") +
            (wQueensideCastle ? "Q" : "") +
            (bKingsideCastle ? "k" : "") +
            (bQueensideCastle ? "q" : "");

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

            nextBoard.EnPassantAttack(fm); 
            nextBoard.SetEnPassant(fm);

            nextBoard.MoveCastleRook(fm);
            nextBoard.UpdateCastleFlags(fm);
            
            if (moveColor == Color.black)
                nextBoard.moveNumber++;

            nextBoard.moveColor = moveColor.FlipColor();// end of the move

            nextBoard.GenerateFen();//apply the move to current board state
            return nextBoard;
        }

        void EnPassantAttack(FigureMoving fm)
        {
            Square midSquare = new Square(enPassant);
            Figure pawn = moveColor == Color.white ? Figure.whitePawn : Figure.blackPawn;

            if (fm.to == midSquare && fm.figure == pawn)// en passant
            {
                InsertFigure(new Square(midSquare.x, fm.from.y), Figure.none);
            }
        }

        void SetEnPassant(FigureMoving fm)
        {
            if (fm.figure == Figure.whitePawn || fm.figure == Figure.blackPawn)
            { 
                if (fm.AbsDeltaY == 2)// jump is only possible if a pawn moves from Y:1 or Y:6 
                {
                    int y = fm.to.y - fm.SignDeltaY;
                    Square midSquare = new Square(fm.to.x, y);
                    enPassant = midSquare.ToString();
                    return;
                }
            }
            enPassant = "-";// en passant only has power during one move
        }

    
        void MoveCastleRook(FigureMoving fm)
        {
            if ((fm.figure == Figure.whiteKing || fm.figure == Figure.blackKing) &&
                fm.AbsDeltaX == 2 && fm.AbsDeltaY == 0)
            {
                Figure rook = fm.figure == Figure.whiteKing ? Figure.whiteRook : Figure.blackRook;

                if (fm.SignDeltaX == 1)// kingside castle
                {
                    InsertFigure(new Square(7, fm.from.y), Figure.none);
                    InsertFigure(new Square(fm.to.x + (fm.SignDeltaX * -1), fm.to.y), rook);
                }
                else if (fm.SignDeltaX == -1)// queenside castle
                {
                    InsertFigure(new Square(0, fm.from.y), Figure.none);
                    InsertFigure(new Square(fm.to.x + (fm.SignDeltaX * -1), fm.to.y), rook);
                }
            }
        }


        void UpdateCastleFlags(FigureMoving fm)
        {
            // we move our king or rook
            Figure king = moveColor == Color.white ? Figure.whiteKing : Figure.blackKing;
            Figure rook = moveColor == Color.white ? Figure.whiteRook : Figure.blackRook;

            if (fm.figure == king)
            {
                DisableQueensideCastle(moveColor);
                DisableKingsideCastle(moveColor);
            }
            else if(fm.figure == rook)
            {
                if(fm.from.x == 0)
                {
                    DisableQueensideCastle(moveColor);
                } 
                else if(fm.from.x == 7)
                {
                    DisableKingsideCastle(moveColor);
                } 
            }
        }
       
        void DisableKingsideCastle(Color color)
        {
            if (color == Color.white)
            {
                wKingsideCastle = false;
            }
            else// if (color == Color.black)
            {
                bKingsideCastle = false;
            }
        }

        void DisableQueensideCastle(Color color)
        {
            if (color == Color.white)
            {
                wQueensideCastle = false;
            }
            else//if (color == Color.black)
            {
                bQueensideCastle = false;
            }
        }


        public bool CanAttackEnemyKing()
        {
            Square enemyKing = FindEnemyKing();
            Moves moves = new Moves(this);  
            
            foreach(FigureOnSquare fs in YieldFigures())
            {
                if (moves.CanMove(new FigureMoving(fs, enemyKing)))// if any of our figure can attack the enemy king
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

        public bool IsCheckAfterMove(FigureMoving fm)
        {
            Board afterBoard = Move(fm); 
            return afterBoard.CanAttackEnemyKing();
        }

        public bool IsCheck()
        {
            Board afterBoard = new Board(fen);
            afterBoard.moveColor = moveColor.FlipColor();
            // now we can work with enemy figures
            return afterBoard.CanAttackEnemyKing();
        }
    }
}
