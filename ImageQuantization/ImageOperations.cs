using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;
///Algorithms Project
///Intelligent Scissors
///

namespace ImageQuantization
{
    /// <summary>
    /// Holds the pixel color in 3 byte values: red, green and blue
    /// </summary>
    public struct RGBPixel
    {
        public byte red, green, blue;
    }
    /// <summary>
    /// Library of static functions that deal with images
    /// </summary>
    public class ImageOperations
    {
        /// <summary>
        /// Open an image and load it into 2D array of colors (size: Height x Width)
        /// </summary>
        /// <param name="ImagePath">Image file path</param>
        /// <returns>2D array of colors</returns>
        public static RGBPixel[,] OpenImage(string ImagePath)
        {
            Bitmap original_bm = new Bitmap(ImagePath);
            int Height = original_bm.Height;
            int Width = original_bm.Width;

            RGBPixel[,] Buffer = new RGBPixel[Height, Width];

            unsafe
            {
                BitmapData bmd = original_bm.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadWrite, original_bm.PixelFormat);
                int x, y;
                int nWidth = 0;
                bool Format32 = false;
                bool Format24 = false;
                bool Format8 = false;

                if (original_bm.PixelFormat == PixelFormat.Format24bppRgb)
                {
                    Format24 = true;
                    nWidth = Width * 3;
                }
                else if (original_bm.PixelFormat == PixelFormat.Format32bppArgb || original_bm.PixelFormat == PixelFormat.Format32bppRgb || original_bm.PixelFormat == PixelFormat.Format32bppPArgb)
                {
                    Format32 = true;
                    nWidth = Width * 4;
                }
                else if (original_bm.PixelFormat == PixelFormat.Format8bppIndexed)
                {
                    Format8 = true;
                    nWidth = Width;
                }
                int nOffset = bmd.Stride - nWidth;
                byte* p = (byte*)bmd.Scan0;
                for (y = 0; y < Height; y++)
                {
                    for (x = 0; x < Width; x++)
                    {
                        if (Format8)
                        {
                            Buffer[y, x].red = Buffer[y, x].green = Buffer[y, x].blue = p[0];
                            p++;
                        }
                        else
                        {
                            Buffer[y, x].red = p[2];
                            Buffer[y, x].green = p[1];
                            Buffer[y, x].blue = p[0];
                            if (Format24) p += 3;
                            else if (Format32) p += 4;
                        }
                    }
                    p += nOffset;
                }
                original_bm.UnlockBits(bmd);
            }
            return Buffer;
        }
        
        /// <summary>
        ///  Some global Data used in the project
        ///  parent array to save every parent for each node
        ///  sz integer holds the number of distinct colours
        ///  vis to mark every node to check if it visited or not
        ///  color array to holds the distinct colors
        ///  flatten array holds every color and it's new color
        /// </summary>
        private static int[] parent;
        private static int D=0;
        private static bool[] vis;
        private static RGBPixel[] colors;
        private static RGBPixel[,,] flatten;
        private static double[] cost;
        /// <summary>
        /// Get the height of the image 
        /// </summary>
        /// <param name="ImageMatrix">2D array that contains the image</param>
        /// <returns>Image Height</returns>
        public static int GetHeight(RGBPixel[,] ImageMatrix)
        {
            return ImageMatrix.GetLength(0);
        }

        /// <summary>
        /// Get the width of the image 
        /// </summary>
        /// <param name="ImageMatrix">2D array that contains the image</param>
        /// <returns>Image Width</returns>
        public static int GetWidth(RGBPixel[,] ImageMatrix)
        {
            return ImageMatrix.GetLength(1);
        }
        /// <summary>
        /// Display the given image on the given PictureBox object
        /// </summary>
        /// <param name="ImageMatrix">2D array that contains the image</param>
        /// <param name="PicBox">PictureBox object to display the image on it</param>
        public static void DisplayImage(RGBPixel[,] ImageMatrix, PictureBox PicBox,int choice)
        {
            // Create Image:
            //==============
            int Height = ImageMatrix.GetLength(0);
            int Width = ImageMatrix.GetLength(1);
            Bitmap ImageBMP = new Bitmap(Width, Height, PixelFormat.Format24bppRgb);
            if (choice == 0)
            {
                unsafe
                {
                    BitmapData bmd = ImageBMP.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadWrite, ImageBMP.PixelFormat);
                    int nWidth = 0;
                    nWidth = Width * 3;
                    int nOffset = bmd.Stride - nWidth;
                    byte* p = (byte*)bmd.Scan0;
                    for (int i = 0; i < Height; i++)
                    {
                        for (int j = 0; j < Width; j++)
                        {
                            p[2] = ImageMatrix[i, j].red;
                            p[1] = ImageMatrix[i, j].green;
                            p[0] = ImageMatrix[i, j].blue;
                            p += 3;
                        }

                        p += nOffset;
                    }
                    ImageBMP.UnlockBits(bmd);
                }
            }
            else
            {
                unsafe
                {
                    BitmapData bmd = ImageBMP.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.ReadWrite, ImageBMP.PixelFormat);
                    int nWidth = 0;
                    nWidth = Width * 3;
                    int nOffset = bmd.Stride - nWidth;
                    byte* p = (byte*)bmd.Scan0;
                    for (int i = 0; i < Height; i++)
                    {
                        for (int j = 0; j < Width; j++)
                        {
                            p[2] = flatten[ImageMatrix[i, j].red, ImageMatrix[i, j].green, ImageMatrix[i, j].blue].red;
                            p[1] = flatten[ImageMatrix[i, j].red, ImageMatrix[i, j].green, ImageMatrix[i, j].blue].green;
                            p[0] = flatten[ImageMatrix[i, j].red, ImageMatrix[i, j].green, ImageMatrix[i, j].blue].blue;
                            p += 3;
                        }

                        p += nOffset;
                    }
                    ImageBMP.UnlockBits(bmd);
                }
            }
            PicBox.Image = ImageBMP;
        }
        /// <summary>
        /// Find the distinct colors
        /// </summary>
        /// <param name="ImageMatrix">2D array that contains the image</param>
        /// <param name="height">Image Height</param>
        /// <param name="width">Image Width</param>
        /// <returns>The Number of Distinct colors</returns>
        private static int Find_dist_points(RGBPixel[,] ImageMatrix, int height, int width)
        {
            D = 0;                                           // -> O(1)
            colors = new RGBPixel[height * width];            // -> O(1)
            bool[, ,] vis = new bool[256, 256, 256];          // -> O(1)
            int r, g, b;                                      // -> O(1)
            for (int i = 0; i < height; i++)                  // -> O(height) where height is the height for the current image.
            {
                for (int j = 0; j < width; j++)               // -> O(width) where height is the height for the current image.
                {
                    r = ImageMatrix[i, j].red;                // -> O(1)
                    g = ImageMatrix[i, j].green;              // -> O(1)
                    b = ImageMatrix[i, j].blue;               // -> O(1)
                    if (vis[r, g, b] == false)                // -> O(1)
                    {
                        vis[r, g, b] = true;                  // -> O(1)
                        colors[D++] = ImageMatrix[i, j];     // -> O(1)
                    }
                }
            }
            return D;                                       // -> O(1)
        }
        /// <summary>
        /// Minimum Spaninng Tree using Prim E log V
        /// </summary>
        /// <param name="k">Number Of Clusters if exist </param>
        /// <returns> List of Edges that is in MST </returns>
        private static void MST_ElogV(ref double sum)
        {
            parent = new int[D];
            cost = new double[D];
            vis = new bool[D];
            FibbHeap fh = new FibbHeap(D);
            HeapNode n;
           for(int i=1;i< D;i++)
            {
                parent[i] = -1;
                cost[i] = 1e9;
                fh.Insert(i, cost[i]);
            }
           parent[0] = 0;
           cost[0] = 0;
            fh.Insert(0,0);
            int v,u;
            double d;
            while(fh.sz!=0)
            {
                n = fh.Extract_min();
                u = n.key;
                vis[u] = true;
                for (int i = 0; i < D;i++ )
                {
                    if (i == u)
                        continue;
                    v = i;
                    double dr, dg;                                  // -> O(1)
                    dr = colors[u].red - colors[v].red;             // -> O(1)
                    dg = colors[u].green - colors[v].green;         // -> O(1)
                    d = colors[u].blue - colors[v].blue;           // -> O(1)

                    dr *= dr;                                       // -> O(1)
                    dg *= dg;                                       // -> O(1)
                    d *= d;                                       // -> O(1)

                    d += dr;                                       // -> O(1)
                    d += dg;                                       // -> O(1)
                    d = Math.Sqrt(d);     
                    if ( d < cost[v] && vis[v]==false)
                    {
                        parent[v] = u;
                        cost[v] = d;
                        fh.decrease_key(v, d);
                    }
                }
            }
            for (int i = 0; i < D; i++) sum += cost[i];
        }
        /// <summary>
        /// Calculate the standard Deviation
        /// </summary>
        /// <param name="Mean">Mean for the current edges</param>
        /// <param name="removed">1D array to check if this Node is Removed or not </param>
        /// <param name="cost">1D array holds the cost for every node </param>
        /// <returns> Standard deviation </returns>
        private static double SD(double Mean,bool[] removed)
        {
            double cn = 0;                                   // - > O(1)
            double tot = 0;                                  // - > O(1)
            for (int i = 1; i < D; i++)                      // - > O(D)
            {
                if (!removed[i])                   // - > O(1)
                {
                    tot += ((cost[i] - Mean) * (cost[i] - Mean));      // - > O(1)
                    cn++;                   // - > O(1)
                }
            }
            return Math.Sqrt(tot / cn);
        }
        /// <summary>
        /// Calculate the Number of clusters
        /// </summary>
        /// <param name="sum">Summation for the current edges</param>
        /// <param name="cost">1D array holds the cost for every node </param>
        /// <returns> Number of clusters </returns>
        private static int calc_K(double sum)
        {
            int cn=D,cnt=0,indx=0;                          // ->O(1)
            double mean, sd, pre = 1e10, maxx = 0;          // ->O(1)             
            bool[] removed = new bool[D];                   // ->O(1)
            while (true)                                    // -> Best O(1) Worst O(D) where D is distinct colors
            {
                mean = (double)sum / cn;                   // ->O(1)
                sd = SD(mean, removed);                    // ->O(D)
                if (pre - sd < 0.0001)                     // ->O(1)
                {
                    break;
                }
                cnt++;                                     // ->O(1)
                maxx = indx = 0;                           // ->O(1)
                for (int i = 1; i < D; i++)                // ->O(D)
                {
                    if (Math.Abs(cost[i] - mean) > maxx && !removed[i])               // ->O(1)
                    {
                        maxx = Math.Abs(cost[i] - mean);                              // ->O(1)
                        indx = i;                                                     // ->O(1)
                    }
                }
                sum -= cost[indx];               // ->O(1)
                cn--;                            // ->O(1)
                removed[indx] = true;            // ->O(1)
                pre = sd;                        // ->O(1)
            }
            return cnt;
        }
        /// <summary>
        /// Minimum Spaninng Tree using Prim V^2
        /// </summary>
        /// <param name="k">Number Of Clusters if exist </param>
        /// <returns> List of Edges that is in MST </returns>
        private static void MST_Vsquare(ref double sum)
        {
            cost = new double[D + 1];                                                     // -> O(1)
            parent = new int[D];                                                          // -> O(1)
            vis = new bool[D];                                                            // -> O(1)
            for (int i = 0; i < D; i++)                                                   // -> O(D) where D is the Numhber of Distinct Colors
            {
                parent[i] = i;                                                            // -> O(1)
                cost[i] = 1e9;                                                            // -> O(1)                                                    
            }
            cost[0] = 0;                                                                   // -> O(1)
            parent[0] = 0;                                                                 // -> O(1)
            int indx = 0, minn = D;                                                        // -> O(1)
            cost[minn] = 1e9;                                                              // -> O(1)
            double d,dr,dg;                                                                       // -> O(1)
            for (int j = 0; j < D - 1; j++)                                                // -> O(D) where D is the Numhber of Distinct Colors
            {
                vis[indx] = true;                                                          // -> O(1)
                minn = D;                                                                  // -> O(1)
                for (int i = 0; i < D; i++)                                                // -> O(D) where D is the Numhber of Distinct Colors
                {                                                
                    dr = colors[indx].red - colors[i].red;             // -> O(1)
                    dg = colors[indx].green - colors[i].green;         // -> O(1)
                    d = colors[indx].blue - colors[i].blue;           // -> O(1)

                    dr *= dr;   dg *= dg;  d *= d;                                           // -> O(1)
                    d += dr; d += dg; d = Math.Sqrt(d);                                      // -> O(1)        
                    if (i != indx && d < cost[i] && !vis[i])                                 // -> O(1)
                    {    cost[i] = d;                                                        // -> O(1)
                        parent[i] = indx;                                                    // -> O(1)
                    }
                    if (i != indx && cost[i] < cost[minn] && !vis[i])                       // -> O(1)
                    {
                        minn = i;                                                           // -> O(1)
                    }
                }
                indx = minn;                                                               // -> O(1)
                sum += cost[indx];
        }    }
        /// <summary>
        /// some global data used in clustering
        /// </summary>
        private static double r,g,b;
        private static List<int>[] adj;
        private static int cnt, id = 0;
        private static int[] ids ;
        private static RGBPixel[] finall;
        /// <summary>
        /// traverse all the connected sub tree and mark in visited and get its sum and number of nodes
        /// </summary>
        /// <param name="u">the root for the current sub tree </param>
        private static void BFS(int u)
        { 
            int co;                                // -> O(1)
            vis[u] = true;
            Queue<int> Q = new Queue<int>();       // -> O(1)
            Q.Enqueue(u);                          // -> O(1)
            while (Q.Count != 0)                   // -> Best case O(1) and Worst Case O(V) where V the number of vertices
            {
                u = Q.Dequeue();                  // -> O(1)
                ids[u] = id;                      // -> O(1)
                cnt++;                            // -> O(1)
                r += colors[u].red;               // -> O(1)
                g += colors[u].green;             // -> O(1)
                b += colors[u].blue;              // -> O(1)
                co = adj[u].Count;                // -> O(1)
                for (int i = 0; i < co; i++)      // -> Best case O(1) and worst case O(co) where co the number of edges (E)
                {
                    if (!vis[adj[u][i]])          // -> O(1)
                    {
                        vis[adj[u][i]] = true;     // -> O(1)
                        Q.Enqueue(adj[u][i]);      // -> O(1)
                    }
                }
            }
        }
        /// <summary>
        /// Remove K-1 Maximal Edges From the given List.
        /// </summary>
        /// <param name="graph">List of edges that is in the MST </param>
        /// <param name="k">the number of clusters  </param>
        private static void Extract_k(int k)
        {
            for (int i = 0; i < k - 1; i++)                                           // -> O(k) where k number of clusters
            {
                double mx = 0;                                                        // -> O(1)
                int to = -1;                                                          // -> O(1)
                for (int j = 1; j <D ; j++)                                           // -> O(c) where c number of edges (D-1)
                {
                    if (cost[j]> mx)                                                 // -> O(1)
                    {
                        mx = cost[j];                                                // -> O(1)
                        to = j;                                                      // -> O(1)
                    }
                }
                parent[to] = to;                                                      // -> O(1)
                cost[to] = -1;
            }
        }
        /// <summary>
        /// Find the representative color of each cluster.
        /// </summary>
        /// <param name="graph">List of edges that is in the MST </param>
        private static void Find_Cluster()
        {
                adj = new List<int>[D];                      // -> O(1)
                for (int i = 0; i < D; i++)                  // -> O(D) where D the number of distinct colors
                {
                    adj[i] = new List<int>(D);               // -> O(1)
                }
                for (int i = 0; i < D; i++)                  // -> O(D) where D the number of distinct colors
                {
                    if (i == parent[i]) continue;            // -> O(1)
                    adj[parent[i]].Add(i);                   // -> O(1)
                    adj[i].Add(parent[i]);                   // -> O(1)
                }
                finall = new RGBPixel[D];                    // -> O(1)
                ids = new int[D];                            // -> O(1)
                vis = new bool[D];                           // -> O(1)
                id = 0;                                      // -> O(1)
                for (int i = 0; i < D; i++)                  // -> O(D)
                {
                if (!vis[i])                                 // -> O(1)
                {
                    cnt = 0;                                 // -> O(1)
                    r = g = b = 0;                           // -> O(1)
                    BFS(i);                                  // -> best case O(1) Worst Case O(E+V) one time only 
                    double tmp = (double)b / cnt;            // -> O(1)
                    finall[id].blue = (byte)tmp;             // -> O(1)
                    tmp = (double)g / cnt;                   // -> O(1)
                    finall[id].green = (byte)tmp;            // -> O(1)
                    tmp = (double)r / cnt;                   // -> O(1)
                    finall[id].red = (byte)tmp;              // -> O(1)
                    id++;                                    // -> O(1)
                }
            }
        }
        /// <summary>
        /// Replace every color by its new color
        /// </summary>
        private static void Replace()
        {
            flatten = new RGBPixel[256,256,256]; // -> O(1)
            int id;                              // -> O(1)
            for(int i=0;i<D;i++)                 // -> O(D)
            {
                id = ids[i];                     // -> O(1)
                flatten[colors[i].red, colors[i].green, colors[i].blue] = finall[id]; // -> O(1)
            }
        }
        public static void Run(ref double sum,ref int Dist_colors,ref int K, RadioButton butt1, RGBPixel[,] ImageMatrix)
        {
            Dist_colors =Find_dist_points(ImageMatrix, GetHeight(ImageMatrix),GetWidth(ImageMatrix)); // O(N^2) where N is the height or the weight of image
            if (butt1.Checked)
            {
               MST_ElogV(ref sum); // O(E log V) where V=D and E= D*D-1 / 2.
            }
            else
            {
             MST_Vsquare(ref sum);  // O(V^2)
            }
            if (K == 0)
            {
                K = calc_K(sum);                     //O(D^2)
            }
            sum = Math.Round(sum, 1);
            Extract_k(K);            //O(K*D)
            Find_Cluster();                //O(D)
            Replace();                           //O(D)
        }
    }
}