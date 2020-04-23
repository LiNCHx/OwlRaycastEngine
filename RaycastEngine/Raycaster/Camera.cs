﻿/**
 * Owain Bell - 2017
 * Code reworked into some simple OOP classes from the well known tutorial - http://lodev.org/cgtutor/raycasting.html 
 * */
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RaycastEngine.MainGame;//annoying dependancy

/**
 * Class that represents a camera in terms of raycasting.  
 * Contains methods to move the camera, and handles projection to,
 * set the rectangle slice position and height,
 */
namespace RaycastEngine
{
    class Camera
    {

        //--camera position, init to start position--//
        private static Vector2 pos = new Vector2(22.5f, 11.5f);

        //--current facing direction, init to values coresponding to FOV--//
        private static Vector2 dir = new Vector2(-1.0f, 0.0f);

        //--the 2d raycaster version of camera plane, adjust y component to change FOV (ratio between this and dir x resizes FOV)--//
        private static Vector2 plane = new Vector2(0.0f, 0.66f);

        //--viewport width and height--//
        private static int w;
        private static int h;

        // World Map
        private static Map map;
        private static int[,] worldMap;
        private static int[,] upMap;
        private static int[,] midMap;

        // Texture Width
        private static int texWidth;

        // Slices
        private static Rectangle[] s;

        // Movement Speed
        private static float moveSpeed = 0.06f;

        // Mouse Sensitivity
        private static float mouseSensitivity = 1f;

        //--cam x pre calc--//
        private static double[] camX;

        //--structs that contain rects and tints for each level or "floor" renderered--//
        Level[] lvls;

        public Camera(int width, int height, int texWid, Rectangle[] slices, Level[] levels)
        {
            w = width;
            h = height;
            texWidth = texWid;
            s = slices;
            lvls = levels;

            //--init cam pre calc array--//
            camX = new double[w];
            PreCalcCamX();

            map = new Map();
            worldMap = map.getGrid();
            upMap = map.getGridUp();
            midMap = map.getGridMid();

            Raycast();
        }

        public void Update()
        {
            Raycast();

            /*   Handle Movement   */
            KeyboardState keyState = Keyboard.GetState();
            MouseState mouseState = Mouse.GetState();

            if (keyState.IsKeyDown(Keys.Left) || keyState.IsKeyDown(Keys.A))
            {
                MoveSideways(moveSpeed);
            }

            if (keyState.IsKeyDown(Keys.Right) || keyState.IsKeyDown(Keys.D))
            {
                MoveSideways(-moveSpeed);
            }

            if (keyState.IsKeyDown(Keys.Up) || keyState.IsKeyDown(Keys.W))
            {
                MoveForward(moveSpeed);
            }

            if (keyState.IsKeyDown(Keys.Down) || keyState.IsKeyDown(Keys.S))
            {
                MoveForward(-moveSpeed);
            }

            Rotate(mouseState.X * mouseSensitivity * -0.001f);

            Mouse.SetPosition(0, 0);
        }

        public void Raycast()
        {
            for (int x = 0; x < w; x++)
            {
                for (int i = 0; i < lvls.Length; i++)
                {
                    int[,] map;
                    if (i == 0) map = worldMap;
                    else if (i == 1) map = midMap;
                    else map = upMap; //if above lvl2 just keep extending up
                    // TODO: Support new Map format
                    CastLevel(x, map, lvls[i].cts, lvls[i].sv, lvls[i].st, i);
                }
            }

        }

        public void CastLevel(int x, int[,] grid, Rectangle[] _cts, Rectangle[] _sv, Color[] _st, int levelNum)
        {
            // Calculate ray position and direction
            double cameraX = camX[x];//x-coordinate in camera space
            double rayDirX = dir.X + plane.X * cameraX;
            double rayDirY = dir.Y + plane.Y * cameraX;

            //--rays start at camera position--//
            double rayPosX = pos.X;
            double rayPosY = pos.Y;

            //which box of the map we're in
            int mapX = (int)rayPosX;
            int mapY = (int)rayPosY;

            // Length of ray from current position to next x or y-side
            double sideDistX;
            double sideDistY;

            //length of ray from one x or y-side to next x or y-side
            double deltaDistX = Math.Sqrt(1 + (rayDirY * rayDirY) / (rayDirX * rayDirX));
            double deltaDistY = Math.Sqrt(1 + (rayDirX * rayDirX) / (rayDirY * rayDirY));
            double perpWallDist;

            //what direction to step in x or y-direction (either +1 or -1)
            int stepX;
            int stepY;

            int hit = 0; //was there a wall hit?
            int side = -1; //was a NS or a EW wall hit?

            //calculate step and initial sideDist
            if (rayDirX < 0)
            {
                stepX = -1;
                sideDistX = (rayPosX - mapX) * deltaDistX;
            }
            else
            {
                stepX = 1;
                sideDistX = (mapX + 1.0 - rayPosX) * deltaDistX;
            }
            if (rayDirY < 0)
            {
                stepY = -1;
                sideDistY = (rayPosY - mapY) * deltaDistY;
            }
            else
            {
                stepY = 1;
                sideDistY = (mapY + 1.0 - rayPosY) * deltaDistY;
            }
            //perform DDA
            while (hit == 0)
            {
                //jump to next map square, OR in x-direction, OR in y-direction
                if (sideDistX < sideDistY)
                {
                    sideDistX += deltaDistX;
                    mapX += stepX;
                    side = 0;
                }
                else
                {
                    sideDistY += deltaDistY;
                    mapY += stepY;
                    side = 1;
                }
                //Check if ray has hit a wall
                if (mapX < 24 && mapY < 24 && mapX > 0 && mapY > 0)
                {
                    if (grid[mapX, mapY] > 0) hit = 1;
                }
                else
                {
                    //hit grid boundary
                    hit = 2;

                    //prevent out of range errors, needs to be improved
                    if (mapX < 0) mapX = 0;
                    else if (mapX > 23) mapX = 23;
                    if (mapY < 0) mapY = 0;
                    else if (mapY > 23) mapY = 23;
                }
            }

            //Calculate distance of perpendicular ray (oblique distance will give fisheye effect!)
            if (side == 0) perpWallDist = (mapX - rayPosX + (1 - stepX) / 2) / rayDirX;
            else perpWallDist = (mapY - rayPosY + (1 - stepY) / 2) / rayDirY;

            //Calculate height of line to draw on screen
            int lineHeight = (int)(h / perpWallDist);

            //calculate lowest and highest pixel to fill in current stripe
            int drawStart = (-lineHeight / 2 + h / 2);
            //int drawEnd = (lineHeight / 2 + h / 2);

            //texturing calculations
            int texNum = grid[mapX, mapY] - 1; //1 subtracted from it so that texture 0 can be used
            if (texNum < 0) texNum = 0; //why?
            lvls[levelNum].currTexNum[x] = texNum;

            //calculate value of wallX
            double wallX; //where exactly the wall was hit
            if (side == 0) wallX = rayPosY + perpWallDist * rayDirY;
            else wallX = rayPosX + perpWallDist * rayDirX;
            wallX -= Math.Floor(wallX);

            //x coordinate on the texture
            int texX = (int)(wallX * texWidth);
            if (side == 0 && rayDirX > 0) texX = texWidth - texX - 1;
            if (side == 1 && rayDirY < 0) texX = texWidth - texX - 1;

            //--some supid hacks to make the houses render correctly--//
            if (side == 0)
            {
                if (texNum == 3)
                    lvls[levelNum].currTexNum[x]++;
                else if (texNum == 4)
                    lvls[levelNum].currTexNum[x]--;

                if (texNum == 1)
                    lvls[levelNum].currTexNum[x] = 4;
                else if (texNum == 2)
                    lvls[levelNum].currTexNum[x] = 3;
            }

            //--set current texture slice to be slice x--//
            _cts[x] = s[texX];

            //--set height of slice--//
            _sv[x].Height = lineHeight;

            //--set draw start of slice--//
            _sv[x].Y = drawStart - lineHeight * levelNum;

            //--due to modern way of drawing using quads this should be down here to ovoid glitches at the edges--//
            if (drawStart < 0) drawStart = 0;
            // if (drawEnd >= h) drawEnd = h - 1;

            //--add a bit of tint to differentiate between walls of a corner--//
            _st[x] = Color.White;
            if (side == 1)
            {
                int wallDiff = 12;
                _st[x].R -= (byte)wallDiff;
                _st[x].G -= (byte)wallDiff;
                _st[x].B -= (byte)wallDiff;
            }
            //--simulates torch light, as if player was carrying a radial light--//
            float lightFalloff = -100; //decrease value to make torch dimmer

            //--sun brightness, illuminates whole level--//
            float sunLight = 300;//global illuminaion

            //--distance based dimming of light--//
            float shadowDepth = (float)Math.Sqrt(perpWallDist) * lightFalloff;
            _st[x].R = (byte)MathHelper.Clamp(_st[x].R + shadowDepth + sunLight, 0, 255);
            _st[x].G = (byte)MathHelper.Clamp(_st[x].G + shadowDepth + sunLight, 0, 255);
            _st[x].B = (byte)MathHelper.Clamp(_st[x].B + shadowDepth + sunLight, 0, 255);

        }

        public void MoveForward(float mSpeed)
        {
            try
            {
                // TODO: Bigger collision boxes
                if (worldMap[(int)(pos.X + dir.X * mSpeed), (int)pos.Y] == 0)
                {
                    pos.X += dir.X * mSpeed;
                }

                if (worldMap[(int)pos.X, (int)(pos.Y + dir.Y * mSpeed)] == 0)
                { 
                    pos.Y += dir.Y * mSpeed; 
                }
            } catch (Exception ex)
            {
                Console.WriteLine("MovementFailed: " + ex.Message);
            }
        }

        public void MoveSideways(float mSpeed)
        {
            float rotX = (float)(dir.X * Math.Cos(1.5) - dir.Y * Math.Sin(1.5));
            float rotY = (float)(dir.X * Math.Sin(1.5) + dir.Y * Math.Cos(1.5));

            if (worldMap[(int)(pos.X + rotX * mSpeed), (int)pos.Y] == 0)
            {
                pos.X += rotX * mSpeed;
            }

            if (worldMap[(int)pos.X, (int)(pos.Y + rotY * mSpeed)] == 0)
            {
                pos.Y += rotY * mSpeed;
            }
        }

        /// <summary>
        /// Rotates camera by rotate speed
        /// </summary>
        /// <param name="rSpeed">Rotate speed</param>
        public void Rotate(float rSpeed)
        {
            //both camera direction and camera plane must be rotated
            double oldDirX = dir.X;
            dir.X = (float)(dir.X * Math.Cos(rSpeed) - dir.Y * Math.Sin(rSpeed));
            dir.Y = (float)(oldDirX * Math.Sin(rSpeed) + dir.Y * Math.Cos(rSpeed));
            double oldPlaneX = plane.X;
            plane.X = (float)(plane.X * Math.Cos(rSpeed) - plane.Y * Math.Sin(rSpeed));
            plane.Y = (float)(oldPlaneX * Math.Sin(rSpeed) + plane.Y * Math.Cos(rSpeed));
        }

        /// <summary>
        /// precalculates camera x coordinate
        /// </summary>
        public static void PreCalcCamX()
        {
            for (int x = 0; x < w; x++)
                camX[x] = 2 * x / (double)w - 1; //x-coordinate in camera space
        }
    }
}
