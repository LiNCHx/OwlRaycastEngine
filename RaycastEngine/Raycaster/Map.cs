using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RaycastEngine.Raycaster
{
    class Map
    {
        private List<int[,]> mapMatrixList;

        public Map(int[,] worldMatrix)
        {
            mapMatrixList = new List<int[,]>();
            mapMatrixList.Add(worldMatrix);
        }

        public void AddLevel(int[,] matrix)
        {
            mapMatrixList.Add(matrix);
        }

        public int[,] GetLevel(int index)
        {
            if (index >= mapMatrixList.Count) { throw new Exception("Unknown Map Level"); }
            return mapMatrixList.ElementAt(index);
        }

        public int[,] GetWorldMatrix()
        {
            if (mapMatrixList.Count == 0) { throw new Exception("World Map is missing"); }
            return mapMatrixList.First();
        }

        public List<int[,]> GetMatrixList()
        {
            return mapMatrixList;
        }

        public int LevelCount()
        {
            return mapMatrixList.Count;
        }

        /*public int[,] getGrid()
        {
            return worldMap;
        }

        public int[,] getGridUp()
        {
            return upMap;
        }

        public int[,] getGridMid()
        {
            return midMap;
        }*/
    }
}