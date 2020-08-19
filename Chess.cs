using System;

namespace Chess
{
    public class Chess
    {
        public string fen { get; private set; }

        // fen - is a standard notation for describing a particular board position of a chess game; provide all the necessary information to restart a game from a particular position
        // note: pawn = "P", knight = "N", bishop = "B", rook = "R", queen = "Q" and king = "K"
        // note: each rank is described, starting with rank 8 and ending with rank 1; within each rank, the contents of each square are described from file "a" through file "h"
        // note: white figures are designated using upper-case letters ("PNBRQK") while black figures use lowercase ("pnbrqk"). Empty squares are noted using digits 1 through 8 (the number of empty squares), and "/" separates ranks
        // note: "w" means White moves next, "b" means Black moves next
        // note: if neither side can castle, this is "-". Otherwise, this has one or more letters: "K" (White can castle kingside), "Q" (White can castle queenside), "k" (Black can castle kingside), and/or "q" (Black can castle queenside). A move that temporarily prevents castling does not negate this notation
        // note: if there's no en passant target square, this is "-". If a pawn has just made a two-square move, this is the position "behind" the pawn. This is recorded regardless of whether there is a pawn in position to make an en passant capture
        // note: number of halfmoves since the last capture or pawn advance. This is used to determine if a draw can be claimed under the fifty-move rule
        // note: the number of the full move. It starts at 1, and is incremented after Black's move
        public Chess(string fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1")// start of game
        {
            this.fen = fen;
        }

        // moves a figure
        // example: Pe2e4 where "e2" is current board position, "e4" - is target board position
        public Chess Move(string move)
        {
            Chess nextChess = new Chess(move);
            return nextChess;
        }

        public char FigureAt(int x, int y)
        {
            return '.';
        }
    }
}
