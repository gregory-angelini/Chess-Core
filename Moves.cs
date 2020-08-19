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
                    {
                        break;
                    }

                case Figure.whiteBishop:
                case Figure.blackBishop:
                    {
                        break;
                    }

                case Figure.whiteKnight:
                case Figure.blackKnight:
                    return CanKnightMove();

                case Figure.whitePawn:
                case Figure.blackPawn:
                    return true;
            }
            return false;
        }

        bool CanKnightMove()
        {
            if (fm.AbsDeltaX == 1 && fm.AbsDeltaY == 2) return true;
            else if (fm.AbsDeltaX == 2 && fm.AbsDeltaY == 1) return true;
            return false;
        }

        bool CanKingMove()
        {
            if(fm.AbsDeltaX <= 1 && fm.AbsDeltaY <= 1) return true;
            return false;
        }

        bool CanStraightMove()
        {
            Square at = fm.from;
            do
            {
                at = new Square(at.x + fm.SignDeltaX, at.y + fm.SignDeltaY);

                if (at == fm.to) return true;// covers a case when we're killing enemy's figure
            } 
            while (board.FigureAt(at) == Figure.none);

            return false;
        }
    }
}
