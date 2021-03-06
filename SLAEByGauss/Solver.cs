﻿using System;
using System.Collections.Generic;
using System.Linq;
using SLAEByGauss.Arguments;

namespace SLAEByGauss
{
    public class Solver
    {
        public double[,] Matrix { get; private set; }
        public double[,] Intact { get; private set; }
        readonly int _rowLength;
        readonly int _colLength;
        public double[] X { get; private set; }
        public double[] E { get; private set; }
        double _determinant;
        private bool HasMultipleOrInconsistent => _determinant == 0;
        readonly int _freeMember;
        readonly int _numberOfbasisVars;

        public Solver(double[,] matrix)
        {
            this.Matrix = matrix;
            this.Intact = matrix;
            _colLength = matrix.GetLength(0);
            _rowLength = matrix.GetLength(1);
            _freeMember = _rowLength - 1;
            X = new double[_rowLength - 1];
            if (_colLength % 2 == 0) _numberOfbasisVars = _colLength / 2;
            else _numberOfbasisVars = (_colLength - 1) / 2;
        }

        public static event EventHandler<SolverArgs> UpdateMatrix;
        public static event EventHandler<SwapRowsArgs> RowsSwaped;
        public static event EventHandler<GridArgs> UpdateGridRepresentation;
        public static event EventHandler<ResultArgs> CalculationComplete;
        public static event EventHandler<ReversePassArgs> ReversePassComplete;   
        
        public void SolveEquasion()
        {
            UpdateGridRepresentation(this, new GridArgs(Matrix, 0));
            UpdateMatrix(this, new SolverArgs(Matrix));
            SwapRows();
            SimpleConversion();
            UpdateGridRepresentation(this, new GridArgs(Matrix, 1));
            CalculateDelta();
            CalculationComplete(this, new ResultArgs(_determinant, CheckConsistency()));
            if (CheckConsistency())
            {
                ReversePass();
                ReversePassComplete(this, new ReversePassArgs(Matrix, X, E, HasMultipleOrInconsistent));

            }
        }
        public bool CheckConsistency()
        {
            for (int i = 0; i < _colLength; i++)
            {
                double leftSum = 0;
                for (int j = 0; j < _rowLength - 1; j++)
                {
                    leftSum += Matrix[i, j];
                }
                if (leftSum == 0 && leftSum != Matrix[i, 4])
                {
                    return false;
                }
            };
            return true;
        }
        public void SimpleConversion()
        {
            int currentRow = 1;
            int numberOfPasses = 3;
            int colPosition = 0;
            bool consistent = true;
            for (int pass = 0; pass < _colLength - 1; pass++)
            {
                int copyRow = 0;
                int targetRow = 0;
                double copyRowMultiplier = 0;
                double targetRowMultiplier = 0;
                string sign = "";
                for (int rowNumber = currentRow; rowNumber < numberOfPasses; rowNumber++)
                {
                    int x = (int)Math.Abs(Matrix[rowNumber, colPosition]);
                    int y = (int)Math.Abs(Matrix[rowNumber + 1, colPosition]);
                    double xRaw = Matrix[rowNumber, colPosition];
                    double D = Gcd(x, y);
                    if (x != 0)
                    {
                        for (int i = 0; i < _rowLength; i++)
                        {
                            if (consistent)
                            {
                                double temp = Matrix[rowNumber + 1, i];
                                if (temp < 0) sign = "-";
                                else sign = "";
                                if (xRaw < 0 && Matrix[rowNumber + 1, colPosition] < 0) temp = -temp;
                                if (xRaw > 0 && Matrix[rowNumber + 1, colPosition] > 0) temp = -temp;
                                if (D > 1)
                                {
                                    copyRowMultiplier = x / D;
                                    targetRowMultiplier = y / D;
                                    Matrix[rowNumber, i] *= targetRowMultiplier;
                                    temp *= copyRowMultiplier;
                                }
                                else
                                {
                                    copyRowMultiplier = x;
                                    targetRowMultiplier = y;
                                    Matrix[rowNumber, i] *= targetRowMultiplier;
                                    temp *= copyRowMultiplier;
                                }
                                Matrix[rowNumber, i] += temp;
                            }
                            else return;
                        }
                    }
                    targetRow = rowNumber + 1;
                    copyRow = rowNumber + 2;
                    UpdateMatrix(this, new SolverArgs(Matrix, targetRow, copyRow, targetRowMultiplier, copyRowMultiplier, sign));
                }
                
                if (currentRow != 0) currentRow--;
                numberOfPasses--;
                colPosition++;
                consistent = CheckConsistency();
            }
        }
        public void SwapRows()
        {
            for (int rowToCheck = 1; rowToCheck < _colLength; rowToCheck++)
            {
                bool workMade = false;
                double temp;
                if (Matrix[rowToCheck, 0] == 0)
                {
                    int currentRow = rowToCheck;
                    int previousRow = currentRow - 1;
                    while (Matrix[previousRow,0] != 0)
                    {
                        for (int j = 0; j < _rowLength; j++)
                        {
                            temp = Matrix[currentRow, j];
                            Matrix[currentRow, j] = Matrix[previousRow, j];
                            Matrix[previousRow, j] = temp;
                        }
                        currentRow--;
                        if (currentRow != 0) previousRow = currentRow - 1;
                        workMade = true;
                    }
                } 
            if(workMade) RowsSwaped(this, new SwapRowsArgs(Matrix));
            }
        }
        public void CalculateDelta()
        {
            _determinant = Matrix[3, 0] * Matrix[2, 1] * Matrix[1, 2] * Matrix[0, 3]; 
        }
        public int Gcd(int x, int y)
        {
            return y == 0 ? x : Gcd(y, x % y);
        }
        public void ReversePass()
        {
            if (_determinant > 0 || _determinant < 0)
            {
                for (int xRow = 0; xRow < X.Length; xRow++)
                {
                    int xNumber = X.Length - xRow - 1;      
                    int xCol = xNumber;                         
                    X[xNumber] = (Matrix[xRow, _freeMember]);
                    for (int j = 0; j < xRow; j++)
                    {
                        int previousXNumber = xNumber + (j + 1); 
                        int previousXCol = xNumber + j + 1; 
                        X[xNumber] -= X[previousXNumber] * Matrix[xRow, previousXCol];
                    }
                    X[xNumber] /= Matrix[xRow, xCol];
                }
            }
            //---------------------------------------------
            // if _determinant = 0 and Slae has an infinity number of solutions
            else
            {
                SolveMultipleSolutions();
            }
            E = new double[_colLength];
            for (int i = 0; i < _colLength; i++)
            {
                E[i] = Intact[i, _freeMember];
                for (int j = 0; j < X.Length; j++)
                {
                    E[i] -= (X[j] * Intact[i, j]);
                }
            }
        }
        public Dictionary<int, int[]> CheckBasis()
        {
            
            Dictionary<int, int[]> basisDict = new Dictionary<int, int[]>();
            int similarRowNumber = -1;
            for (int i = 0; i < _numberOfbasisVars; i++)
            {
                for (int row = 0; row < _colLength; row++)
                {
                    double[] currentRow = new double[_rowLength];
                    if (row != similarRowNumber)
                    {
                        for (int c = 0; c < _rowLength; c++)
                        {
                            currentRow[c] = Matrix[row, c];
                        }
                        for (int j = 0; j < _rowLength - 1; j++)
                        {
                            if (Matrix[row, j] < 0 || Matrix[row, j] > 0)
                            {   
                                if (!basisDict.Keys.Contains(j)) basisDict.Add(j, new int[] { row, j });
                                break;
                            }
                        }
                    };
                    similarRowNumber = CheckSimilarRow(currentRow, row);
                }
            }
            return basisDict;
        }
        public void SolveMultipleSolutions ()
        {
            Dictionary<int, int[]> basisCoords = CheckBasis();

            for (int col = _colLength; col > 0; col--)
            {
                foreach (var x in basisCoords)
                {
                    X[x.Key] = Matrix[x.Value[0], _freeMember];
                    for (int i = 1; i < _colLength - x.Value[1]; i++)
                    {
                        int xToMinus = x.Value[1] + i;
                        if (basisCoords.Keys.Contains(xToMinus))
                        {
                            X[x.Key] -= X[xToMinus] * Matrix[x.Value[0], xToMinus];
                        } else X[xToMinus] = 0;
                    }
                    X[x.Key] /= Matrix[x.Value[0], x.Value[1]];
                }
            }

        }
        public int CheckSimilarRow(double[] currentRow, int row)
        {
            double[] temp;
            int similarRowNumber = 0;
            for (int i = 0; i < _colLength; i++)
            {
                if (i != row)
                {
                    temp = new double[currentRow.Length];
                    for (int j = 0; j < _rowLength; j++)
                    {
                        if (currentRow[j] < Matrix[i, j] || currentRow[j] > Matrix[i, j])
                        {
                            temp = null;
                            break;
                        }
                        temp[j] = currentRow[j];
                    }
                    if (temp != null)
                    {
                        similarRowNumber = i;
                        return similarRowNumber;
                    };
                }
            }
            return similarRowNumber;
        }
    }
}
