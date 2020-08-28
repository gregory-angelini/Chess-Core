using System;
using System.Collections.Generic;

namespace ChessCore
{
    public class Chess
    {
        // fen - is a standard notation for describing a particular board position of a chess game; provide all the necessary information to restart a game from a particular position
        // note: pawn = "P", knight = "N", bishop = "B", rook = "R", queen = "Q" and king = "K"
        // note: each rank is described, starting with rank 8 and ending with rank 1; within each rank, the contents of each square are described from file "a" through file "h"
        // note: white figures are designated using upper-case letters ("PNBRQK") while black figures use lowercase ("pnbrqk"). Empty squares are noted using digits 1 through 8 (the number of empty squares), and "/" separates ranks
        // note: "w" means White moves next, "b" means Black moves next
        // note: if neither side can castle, this is "-". Otherwise, this has one or more letters: "K" (White can castle kingside), "Q" (White can castle queenside), "k" (Black can castle kingside), and/or "q" (Black can castle queenside). A move that temporarily prevents castling does not negate this notation
        // note: if there's no en passant target square, this is "-". If a pawn has just made a two-square move, this is the position "behind" the pawn. This is recorded regardless of whether there is a pawn in position to make an en passant capture
        // note: number of halfmoves since the last capture or pawn advance. This is used to determine if a draw can be claimed under the fifty-move rule
        // note: the number of the full move. It starts at 1, and is incremented after Black's move
        public string fen { get; private set; }// current game state in fen notation
        Board board;
        Moves moves;

        bool curPlayerInCheck = false;
        bool curPlayerInCheckmate = false;
        bool curPlayerInStalemate = false;

        public Chess(string fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1")// start of game
        {
            this.fen = fen;
            board = new Board(fen);
            moves = new Moves(board);
        }
        
        Chess(Board board)
        {
            this.board = board;
            fen = board.fen;
            moves = new Moves(board);
        }

        // moves a figure
        // example: Pe2e4 where "P" is pawn, "e2" is current board position, "e4" - is target board position; Pe7e8Q (promotion to Queen)
        public Chess Move(string move)
        {
            FigureMoving fm = new FigureMoving(move);
            
            if(!moves.CanMove(fm))
                return this;
            
            Board nextBoard = board.Move(fm);
            Chess nextChess = new Chess(nextBoard);
            nextChess.UpdateKingStatus();
            return nextChess;
        }

        public Figure FigureAt(int x, int y)
        {
            Square square = new Square(x, y);
            return board.FigureAt(square);
        }
       
        public List<string> GetAllMoves()
        {
            List<FigureMoving> allMoves = FindAllMoves();
            List<string> list = new List<string>();

            foreach (FigureMoving fm in allMoves)
                list.Add(fm.ToString());
                
            return list;
        }

        public IEnumerable<string> YieldAllMoves()
        {
            List<FigureMoving> allMoves = FindAllMoves();

            foreach (FigureMoving fm in allMoves)
                yield return fm.ToString();
        }
         
        public bool CanMove(Figure figure, int figureX, int figureY, int targetX, int targetY)
        {
            Square from = new Square(figureX, figureY);
            Square to = new Square(targetX, targetY);

            FigureOnSquare fs = new FigureOnSquare(figure, from);
            FigureMoving fm = new FigureMoving(fs, to);
           
            if (moves.CanMove(fm))
            {
                return true;
            }
            return false;
        }

        void UpdateKingStatus()
        {  
            if (board.IsOurKingInCheck())
            {
                curPlayerInCheck = true;

                foreach (FigureOnSquare fs in board.YieldFigures())
                {
                    foreach (Square to in Square.YieldSquares())
                    {
                        FigureMoving fm = new FigureMoving(fs, to);
                       
                        if (moves.CanMove(fm))
                        {
                            // we have a move to avoid checkmate
                            return;
                        }
                    }
                }
                curPlayerInCheckmate = true;
            }
            else
            {
                if (FindAllMoves().Count == 0)
                {
                    curPlayerInStalemate = true;
                }
            }
        }

        List<FigureMoving> FindAllMoves()
        {
            List<FigureMoving> allMoves = new List<FigureMoving>();
            FigureMoving fm;

            foreach (FigureOnSquare fs in board.YieldFigures())
            {
                foreach (Square to in Square.YieldSquares())
                {
                    fm = new FigureMoving(fs, to);
                   
                    if (moves.CanMove(fm))
                    {
                        allMoves.Add(fm);
                    }
                }
            }
            return allMoves;
        }

        public bool IsCheckmate()
        {
            return curPlayerInCheckmate;
        }

        public bool IsCheck()
        {
            return curPlayerInCheck;
        }

        public bool IsStalemate()
        {
            return curPlayerInStalemate;
        }

        public bool IsEnPassant(out int x, out int y)
        {
            Square midSquare = new Square(board.EnPassant);
            x = midSquare.x;
            y = midSquare.y;
            return board.EnPassant.Length > 0;
        }

        public static string SquarePosToSquareName(int x, int y)
        {
            return Square.GetName(x, y);
        }

        public static void SquareNameToSquarePos(string name, out int x, out int y)
        {
            Square square = new Square(name);
            x = square.x;
            y = square.y;
        }

        // who has a turn?
        public Color GetCurrentPlayerColor()
        {
            return board.currentPlayerColor;
        }

        public static Color GetFigureColor(Figure figure)
        {
            return figure.GetColor();
        }
    }
}
