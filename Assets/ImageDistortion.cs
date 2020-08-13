namespace OpenCvSharp.Demo
{
    using UnityEngine;
    using OpenCvSharp;

    public class ImageDistortion : WebCamera
    {
        public float x;
        public float y;
        public string text;

        //private Mat grid_x = new Mat();
        //private Mat grid_y = new Mat();
        private bool gridSet;

        private void Start()
        {
            gridSet = false;
            x = 15.0f;
            y = 15.0f;
            //text = "";
        }

        protected override void Awake()
        {
            base.Awake();
            this.forceFrontalCamera = false;


        }
        
        //private void Update()
        //{
        //    if (Input.GetAxis("Axis 1") > 0f)
        //        text = "Axis 1 positive";
        //    else if (Input.GetAxis("Axis 1") < 0f)
        //        text = "Axis 1 negative";
        //    else
        //        text = "";

        //    if (Input.GetAxis("Axis 2") > 0f)
        //        text = "Axis 2 positive";
        //    else if (Input.GetAxis("Axis 1") < 0f)
        //        text = "Axis 2 negative";
        //    else
        //        text = "";

        //    if (Input.GetAxis("Axis 5") > 0f)
        //        text = "Axis 5 positive";
        //    else if (Input.GetAxis("Axis 1") < 0f)
        //        text = "Axis 5 negative";
        //    else
        //        text = "";

        //    if (Input.GetAxis("Axis 6") > 0f)
        //        text = "Axis 6 positive";
        //    else if (Input.GetAxis("Axis 6") < 0f)
        //        text = "Axis 6 negative";
        //    else
        //        text = "";

        //    if (Input.GetKey(KeyCode.JoystickButton0) == true)
        //        text = "Button 0 pressed";
        //    else
        //        text = "";

        //    if (Input.GetKey(KeyCode.JoystickButton1) == true)
        //        text = "Button 1 pressed";
        //    else
        //        text = "";

        //    if (Input.GetKey(KeyCode.JoystickButton2) == true)
        //        text = "Button 2 pressed";
        //    else
        //        text = "";

        //    if (Input.GetKey(KeyCode.JoystickButton3) == true)
        //        text = "Button 3 pressed";
        //    else
        //        text = "";

        //}

        // Our sketch generation function
        protected override bool ProcessTexture(WebCamTexture input, ref Texture2D output)
        {
            
            Mat img = Unity.TextureToMat(input, TextureParameters);

            ////Convert image to grayscale
            //Mat imgGray = new Mat();
            //Cv2.CvtColor(img, imgGray, ColorConversionCodes.BGR2GRAY);


            int w = img.Width;
            int h = img.Height;
            Debug.Log("Input Image Size: " + w + " x " + h);

            // Making square image
            int s = (w < h) ? w : h;
            OpenCvSharp.Rect square_region = new OpenCvSharp.Rect((w / 2 - s / 2), (h / 2 - s / 2), (w / 2 + s / 2), (h / 2 + s / 2));
            Mat square_img = new Mat(img, square_region);



            //// Zooming Code
            //OpenCvSharp.Rect crop_region = new OpenCvSharp.Rect((int)x, (int)y, (int)(w - 2 * x), (int)(h - 2 * y));
            //Mat cropped_img = new Mat(square_img, crop_region);

            //Mat zoom_output = new Mat();
            //Size size = new Size(w, h);
            //Cv2.Resize(cropped_img, zoom_output, size, 0, 0, OpenCvSharp.InterpolationFlags.Cubic);

            Mat cardboard = new Mat();
            Cardboardize(ref square_img, ref cardboard);

            // result, passing output texture as parameter allows to re-use it's buffer
            // should output texture be null a new texture will be created
            output = Unity.MatToTexture(cardboard, output);
            return true;
        }

        private void Cardboardize(ref Mat src, ref Mat dst)
        {
            // Calculate Center Point of input image
            int n = src.Cols;
            int c = n / 2;

            // create a cartesian coordinate mesh of pixels.
            // generates 2 matrices xi and yi, each of size n*n 
            Mat xi = new Mat(n, n, MatType.CV_32FC1);
            Mat yi = new Mat(n, n, MatType.CV_32FC1);
            meshgrid(ref xi, ref yi);

            // normalizing indices between -n and n
            Mat xt = xi - c;
            Mat yt = yi - c;

            // Transposing
            xt = xt.T();
            yt = yt.T();
            
            if (!xt.IsContinuous())
                xt = xt.Clone();

            if (!yt.IsContinuous())
                yt = yt.Clone();

            xt = xt.Reshape(0, xt.Rows * xt.Cols);
            yt = yt.Reshape(0, yt.Rows * yt.Cols);

            Mat r = new Mat();
            Mat theta = new Mat();
            Cv2.CartToPolar(xt, yt, r, theta);


            //Cv2.CreateSuperResolution_BTVL1();


            Mat s = r + 0.00001f * r.Mul(r.Mul(r));

            Mat ut = new Mat();
            Mat vt = new Mat();
            Cv2.PolarToCart(s, theta, ut, vt);

            Mat u = new Mat();
            Mat v = new Mat();

            if (!ut.IsContinuous())
                ut = ut.Clone();

            if (!vt.IsContinuous())
                vt = vt.Clone();

            u = ut.Reshape(0, n) + c;
            v = vt.Reshape(0, n) + c;
            u = u.T();
            v = v.T();

            Mat dist = new Mat();
            Cv2.Remap(src, dist, u, v, InterpolationFlags.Linear, BorderTypes.Constant);

            string devicenames = "";
            for (int i = 0; i < WebCamTexture.devices.Length; ++i)
            {
                //if (!WebCamTexture.devices[i].isFrontFacing)
                //{
                //    devicenames = devicenames + WebCamTexture.devices[i].name;
                //    devicenames = devicenames + " , ";
                //}

                devicenames = devicenames + WebCamTexture.devices[i].name;
                devicenames = devicenames + " , ";

            }
            Scalar color = new Scalar(0, 0, 255);
            Point p = new Point(50,c);
            Debug.Log("Number of Cameras Found: " + WebCamTexture.devices.Length);
            //Cv2.PutText(dist, devicenames, p, HersheyFonts.HersheyPlain, 1, color, 2);

            Cv2.HConcat(dist, dist, dst);
        }


        private void meshgrid(ref Mat grid_x, ref Mat grid_y)
        {
            int n = grid_x.Rows;
            Mat row = new Mat(1, n, MatType.CV_32FC1);
           
            for(int i = 1; i <= n; ++i)
            {
                row.Set<float>(0, i, (float)i);
            }
            Cv2.Repeat(row, n, 1, grid_x);

            grid_x.T().CopyTo(grid_y);
        }



    }
}