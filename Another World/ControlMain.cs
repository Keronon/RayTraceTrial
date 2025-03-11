using SFML.System;
using SFML.Window;
using SFML.Graphics;

namespace Another_World
{
    internal class ControlMain
    {
        private ControlMain() { }

        private const string windowTitle  = "Another World";
        private const string outputShader = "../../../OutputShader.frag";
        private const string panorama     = "../../../Panorama.jpg";

        private const string u_resolution  = "u_resolution"; 
        private const string u_skybox      = "u_skybox";     
        private const string u_pos         = "u_pos";        
        private const string u_mouse       = "u_mouse";      
        private const string u_time        = "u_time";       
        private const string u_sample_part = "u_sample_part";
        private const string u_seed1       = "u_seed1";      
        private const string u_seed2       = "u_seed2";
        private const string u_sample      = "u_sample";     

        private static VideoMode videoMode;
        private static int w;
        private static int h;

        private static int      mouseX;
        private static int      mouseY;
        private static bool     mouseHidden;
        private static float    mouseSensitivity;
        private static float    speed;
        private static Vector3f pos;
        private static int      framesStill;

        private static RenderTexture firstTexture;
        private static Sprite        firstTextureSprite;
        private static Sprite        firstTextureSpriteFlipped;
        private static RenderTexture outputTexture;
        private static Sprite        outputTextureSprite;
        private static Sprite        outputTextureSpriteFlipped;

        private static readonly bool[]       wasdUD;
        private static readonly Clock        clock;
        private static readonly RenderWindow window;
        private static readonly Shader       shader;
        private static readonly Random       random;

        static ControlMain()
        {
            videoMode = new(VideoMode.DesktopMode.Width / 2, VideoMode.DesktopMode.Height / 2);
            w = (int)videoMode.Width;
            h = (int)videoMode.Height;

            mouseX           = w / 2;
            mouseY           = h / 2;
            mouseHidden      = true;
            mouseSensitivity = 3.0f;
            speed            = 0.1f;
            pos              = new(-5.0f, 0.0f, 0.0f);
            framesStill      = 1;

            window = new(videoMode, windowTitle, Styles.Titlebar | Styles.Close);
            window.SetFramerateLimit(60);
            window.SetMouseCursorVisible(false);

            firstTexture = new((uint)w, (uint)h);
            firstTextureSprite = new(firstTexture.Texture);
            firstTextureSpriteFlipped = new(firstTexture.Texture) { Scale = new Vector2f(1, -1), Position = new Vector2f(0, h) };

            outputTexture = new((uint)w, (uint)h);
            outputTextureSprite = new(outputTexture.Texture);
            outputTextureSpriteFlipped = new(firstTexture.Texture) { Scale = new Vector2f(1, -1), Position = new Vector2f(0, h) };

            shader = new(null, null, outputShader);
            shader.SetUniform(u_resolution, new Vector2f(w, h));
            shader.SetUniform(u_skybox    , new Texture(panorama));

            random = new();
            clock  = new();
            wasdUD = new bool[6];

            window.Closed += (obj, eArgs) => window.Close();
            window.MouseMoved += (obj, eArgs) =>
            {
                if (mouseHidden)
                {
                    int mx = eArgs.X - w / 2;
                    int my = eArgs.Y - h / 2;
                    mouseX += mx;
                    mouseY += my;
                    Mouse.SetPosition(new Vector2i(w / 2, h / 2), window);
                    if (mx != 0 || my != 0) framesStill = 1;
                }
            };
            window.MouseButtonPressed += (obj, eArgs) =>
            {
                if (!mouseHidden) framesStill = 1;
                window.SetMouseCursorVisible(false);
                mouseHidden = true;
            };
            window.KeyPressed += (obj, eArgs) =>
            {
                if (eArgs.Code == Keyboard.Key.Escape)
                {
                    window.SetMouseCursorVisible(true);
                    mouseHidden = false;
                }
                else if (eArgs.Code == Keyboard.Key.W     ) wasdUD[0] = true;
                else if (eArgs.Code == Keyboard.Key.A     ) wasdUD[1] = true;
                else if (eArgs.Code == Keyboard.Key.S     ) wasdUD[2] = true;
                else if (eArgs.Code == Keyboard.Key.D     ) wasdUD[3] = true;
                else if (eArgs.Code == Keyboard.Key.Space ) wasdUD[4] = true;
                else if (eArgs.Code == Keyboard.Key.LShift) wasdUD[5] = true;
            };
            window.KeyReleased += (obj, eArgs) =>
            {
                     if (eArgs.Code == Keyboard.Key.W     ) wasdUD[0] = false;
                else if (eArgs.Code == Keyboard.Key.A     ) wasdUD[1] = false;
                else if (eArgs.Code == Keyboard.Key.S     ) wasdUD[2] = false;
                else if (eArgs.Code == Keyboard.Key.D     ) wasdUD[3] = false;
                else if (eArgs.Code == Keyboard.Key.Space ) wasdUD[4] = false;
                else if (eArgs.Code == Keyboard.Key.LShift) wasdUD[5] = false;
            };
        }

        public static int Run(CancellationToken token)
        {
            while (window.IsOpen)
            {
                if (token.IsCancellationRequested) break;
                window.DispatchEvents();

                if (mouseHidden)
                {
                    float mx = ((float)mouseX / w - 0.5f) * mouseSensitivity;
                    float my = ((float)mouseY / h - 0.5f) * mouseSensitivity;
                    Vector3f dir = new (0.0f, 0.0f, 0.0f);
                    Vector3f dirTemp;

                         if (wasdUD[0]) dir  = new Vector3f( 1.0f,  0.0f, 0.0f);
                    else if (wasdUD[2]) dir  = new Vector3f(-1.0f,  0.0f, 0.0f);
                         if (wasdUD[1]) dir += new Vector3f( 0.0f, -1.0f, 0.0f);
                    else if (wasdUD[3]) dir += new Vector3f( 0.0f,  1.0f, 0.0f);

                    dirTemp.Z = dir    .Z * (float)Math.Cos(-my) - dir    .X * (float)Math.Sin(-my);
                    dirTemp.X = dir    .Z * (float)Math.Sin(-my) + dir    .X * (float)Math.Cos(-my);
                    dirTemp.Y = dir    .Y;
                    dir    .X = dirTemp.X * (float)Math.Cos( mx) - dirTemp.Y * (float)Math.Sin( mx);
                    dir    .Y = dirTemp.X * (float)Math.Sin( mx) + dirTemp.Y * (float)Math.Cos( mx);
                    dir    .Z = dirTemp.Z;
                    pos += dir * speed;

                         if (wasdUD[4]) pos.Z -= speed;
                    else if (wasdUD[5]) pos.Z += speed;

                    for (int i = 0; i < 6; i++)
                    {
                        if (wasdUD[i])
                        {
                            framesStill = 1;
                            break;
                        }
                    }

                    shader.SetUniform(u_pos        , pos);
                    shader.SetUniform(u_mouse      , new Vector2f(mx, my));
                    shader.SetUniform(u_time       , clock.ElapsedTime.AsSeconds());
                    shader.SetUniform(u_sample_part, 1.0f / framesStill);
                    shader.SetUniform(u_seed1      , new Vector2f((float)random.NextDouble(), (float)random.NextDouble()) * 999.0f);
                    shader.SetUniform(u_seed2      , new Vector2f((float)random.NextDouble(), (float)random.NextDouble()) * 999.0f);
                }

                if (framesStill % 2 == 1)
                {
                    shader.SetUniform(u_sample, firstTexture.Texture);
                    outputTexture.Draw(firstTextureSpriteFlipped, new RenderStates(shader));
                    window.Draw(outputTextureSprite);
                }
                else
                {
                    shader.SetUniform(u_sample, outputTexture.Texture);
                    firstTexture.Draw(outputTextureSpriteFlipped, new RenderStates(shader));
                    window.Draw(firstTextureSprite);
                }

                window.Display();
                framesStill++;
            }
            return 0;
        }

        private static Sprite GetSimpleSprite(uint w, uint h)
        {
            RenderTexture renderTexture = new(w, h);

            // drawing uses the same functions
            renderTexture.Clear(Color.White);
            renderTexture.Draw([
                new Vertex(new(0    , 0), Color.Cyan),
                new Vertex(new(w    , 0), Color.Magenta),
                new Vertex(new(w / 2, h), Color.Yellow)
            ], PrimitiveType.TriangleFan);

            // draw it to the window
            return new Sprite(renderTexture.Texture);
        }
    }
}