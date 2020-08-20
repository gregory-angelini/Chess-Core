using System;
using System.Collections.Generic;
using System.Text;

namespace Chess
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
                   fm.figure.GetColor() == board.moveColor;
        }

        bool CanMoveTo()
        {
            return fm.from.OnBoard() &&
                   fm.from != fm.to && 
                   board.FigureAt(fm.to).GetColor() != board.moveColor;//covers a case when we're going on empty square
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
                   CanPawnJump(stepY);// ||
                   //CanPawnAttack(stepY);

        }

        bool CanPawnAttack(int stepY)
        {
            if ((board.FigureAt(fm.to) != Figure.none) &&
               (fm.AbsDeltaX == 1) &&
               (fm.DeltaY == stepY))
                return true;
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
            if (fm.AbsDeltaX == 1 && fm.AbsDeltaY == 2) return true;
            else if (fm.AbsDeltaX == 2 && fm.AbsDeltaY == 1) return true;
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
            if(fm.AbsDeltaX <= 1 && fm.AbsDeltaY <= 1) return true;
            return false;
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
