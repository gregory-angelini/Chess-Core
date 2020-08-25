using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace ChessCore
{
    class Moves
    {
        FigureMoving fm;
        Board board; 

        public Moves(Board board)
        {
            this.board = board;
        }

        public bool CanMove(FigureMoving fm)
        {
            this.fm = fm;

            return CanMoveFrom() &&
                   CanMoveTo() &&
                   CanFigureMove();
        }

        bool CanMoveFrom()
        {
            return fm.from.OnBoard() &&
                   board.FigureAt(new Square(fm.from.x, fm.from.y)) == fm.figure &&
                   fm.figure.GetColor() == board.currentPlayerColor;// we can only move our figures 
        }

        bool CanMoveTo()
        {
            return fm.from.OnBoard() &&
                   fm.from != fm.to && 
                   board.FigureAt(fm.to).GetColor() != board.currentPlayerColor;//covers a case when we're going on empty square
        }

        bool CanKingCastlingKingside()
        {
            /* kingside
                white case
                k   .   .   r
                e1  f1  g1  h1

                black case
                k   .   .   r
                e8  f8  g8  h8 */

            /* queenside
                white case
                r   .   .   .   k
                a1  b1  c1  d1  e1

                black case
                r   .   .   .   k
                a8  b8  c8  d8  e8 */

            // 1. Both the king and the rook may never have moved during the game
            // 2. There are no pieces between the king and the rook
            // 3. The king is not in check
            // 4. The king does not cross over a square that is attacked by the opponent's pieces
            // 5. The castling move cannot end with the king in check
            if (!board.IsOurKingInCheck())   
            { 
                return IsCastlingAllowed();
            }
            return false;
        }

        bool IsCastlingAllowed()
        {
            bool isKingsideCastlingAllowed = board.currentPlayerColor == Color.white ? board.wKingsideCastling : board.bKingsideCastling;
            bool isQueensideCastlingAllowed = board.currentPlayerColor == Color.white ? board.wQueensideCastling : board.bQueensideCastling;

            if (fm.AbsDeltaX == 2 && fm.AbsDeltaY == 0)
            {
                if (fm.SignDeltaX == 1 && // short; move right
                    isKingsideCastlingAllowed)
                {
                    if (IsWayIsSafe(fm.SignDeltaX))
                    {
                        fm.castling = board.currentPlayerColor == Color.white ? 'K' : 'k';
                        return true;
                    }
                }
                else if (fm.SignDeltaX == -1 &&// long; move left
                         isQueensideCastlingAllowed)
                {
                    if (IsWayIsSafe(fm.SignDeltaX))
                    {
                        fm.castling = board.currentPlayerColor == Color.white ? 'Q' : 'q';
                        return true;
                    }
                }
            }
            return false;
        }

        bool IsWayIsSafe(int offset)
        {
            Square nearSquare = new Square(fm.from.x + offset, fm.from.y);
            Square farSquare = new Square(fm.from.x + (offset * 2), fm.from.y);

            if (board.FigureAt(nearSquare) == Figure.none &&
                board.FigureAt(farSquare) == Figure.none)
             {
                FigureMoving fm1 = new FigureMoving(new FigureOnSquare(fm.figure, fm.from), nearSquare);
                FigureMoving fm2 = new FigureMoving(new FigureOnSquare(fm.figure, fm.from), farSquare);

                if (!board.IsOurKingInCheckAfterMove(fm1) &&
                    !board.IsOurKingInCheckAfterMove(fm2))
                {
                    return true;
                }
            }
            return false;
        } 

        bool CanFigureMove()
        {
            switch(fm.figure)
            {
                case Figure.whiteKing:
                case Figure.blackKing:
                    return CanKingMove();

                case Figure.whiteQueen:
                case Figure.blackQueen:
                    return CanStraightMove();

                case Figure.whiteRook:
                case Figure.blackRook:
                    return CanStraightHVMove();

                case Figure.whiteBishop:
                case Figure.blackBishop:
                    return CanStraightDMove();

                case Figure.whiteKnight:
                case Figure.blackKnight:
                    return CanKnightMove();

                case Figure.whitePawn:
                case Figure.blackPawn:
                    return CanPawnMove();
            }
            return false;
        }

        // Generally the pawn moves forward only, one square at a time. An exception is the first time a pawn is moved, it may move forward two squares. The pawn captures an opposing piece by moving diagonally one square - it cannot capture by moving straight ahead
        bool CanPawnMove()
        {
            if (fm.from.y < 1 || fm.from.y > 6)// pawn cannot be on top or on bottom lines
                return false;

            int stepY = fm.figure.GetColor() == Color.white ? 1 : -1;// pawn can move up or down depends on color

            return CanPawnGo(stepY) ||
                   CanPawnJump(stepY) ||
                   CanPawnAttack(stepY);
        }

        bool CanPawnAttack(int stepY)
        {
            if ((board.FigureAt(fm.to) != Figure.none) &&
               (fm.AbsDeltaX == 1) &&
               (fm.DeltaY == stepY))
                return true;
            return CanEnPassant(stepY);
        }

        bool CanEnPassant(int stepY)
        {
            /* 1. A pawn must move two squares from its initial position in a single move
             * 2. An opposing pawn must be attacking the square the first pawn moved over
             * 3. The first pawn can be captured as if it moved only one square
             * 4. The capture can only be made at the opponent's next move. If the capture is not made, the first pawn is safe from en passant capture for the remainder of the game */
             
            if (board.EnPassant.Length == 2)
            {
                Square midSquare = new Square(board.EnPassant);
            
                if(midSquare == fm.to &&
                  (board.FigureAt(fm.to) == Figure.none) &&
                  (fm.AbsDeltaX == 1) &&
                  (fm.DeltaY == stepY))
                    return true;
            }
            return false;
        }

        bool CanPawnJump(int stepY)
        {
            if ((board.FigureAt(fm.to) == Figure.none) &&
                (fm.DeltaX == 0) &&
                (fm.DeltaY == 2 * stepY) &&// control jump distance 
                (fm.from.y == 1 || fm.from.y == 6) && // allow jump only from start position
                (board.FigureAt(new Square(fm.from.x, fm.from.y + stepY)) == Figure.none))// disallow jumping over figures
                return true;
            return false;
        }

        bool CanPawnGo(int stepY)
        {
            if ((board.FigureAt(fm.to) == Figure.none) &&
                (fm.DeltaX == 0) &&
                (fm.DeltaY == stepY))
                return true;
            return false;
        }

        // The Rook moves in a straight line either horizontally or vertically through any number of unoccupied squares
        bool CanStraightHVMove()
        {
            return (fm.SignDeltaX == 0 || fm.SignDeltaY == 0) &&
                    CanStraightMove();
        }

        // The kNight is the only piece on the board that may jump over other pieces. The knight moves two squares horizontally or vertically and then one more square at a right-angle
        bool CanKnightMove()
        {
            if (fm.AbsDeltaX == 1 && fm.AbsDeltaY == 2) 
                return true;
            else if (fm.AbsDeltaX == 2 && fm.AbsDeltaY == 1) 
                return true;
            return false;
        }

        // The Bishop moves in a straight line diagonally on the board
        bool CanStraightDMove()
        {
            return (fm.SignDeltaX != 0 && fm.SignDeltaY != 0) &&
                    CanStraightMove();
        }

        // The king can move to any adjacent square
        bool CanKingMove()
        {
            if (fm.AbsDeltaX <= 1 && fm.AbsDeltaY <= 1) 
                return true;
            return CanKingCastlingKingside();
        }

        // The Queen moves in a straight line - either vertically, horizontally or diagonally
        bool CanStraightMove()
        {
            Square at = fm.from;
            do
            {
                at = new Square(at.x + fm.SignDeltaX, at.y + fm.SignDeltaY);

                if (at == fm.to) return true;// covers a case when we're killing enemy's figure
            } 
            while (at.OnBoard() &&
                   board.FigureAt(at) == Figure.none);

            return false;
        }
    }
}
