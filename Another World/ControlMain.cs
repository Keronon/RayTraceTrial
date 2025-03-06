using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace Another_World
{
    internal class ControlMain
    {
        public static int Run(CancellationToken token)
        {
            int w                  = 1920;
            int h                  = 1080;
            int mouseX             = w / 2;
            int mouseY             = h / 2;
            float mouseSensitivity = 3.0f;
            float speed            = 0.1f;
            bool mouseHidden       = true;
            bool[] wasdUD          = new bool[6];
            Vector3f pos           = new(-5.0f, 0.0f, 0.0f);
            Clock clock            = new();
            int framesStill        = 1;

            RenderWindow window = new(new VideoMode((uint)w, (uint)h), "Ray tracing", Styles.Titlebar | Styles.Close);
            window.SetFramerateLimit(60);
            window.SetMouseCursorVisible(false);

            RenderTexture firstTexture       = new((uint)w, (uint)h);
            Sprite firstTextureSprite        = new(firstTexture.Texture);
            Sprite firstTextureSpriteFlipped = new(firstTexture.Texture) { Scale = new Vector2f(1, -1), Position = new Vector2f(0, h) };

            RenderTexture outputTexture       = new((uint)w, (uint)h);
            Sprite outputTextureSprite        = new(outputTexture.Texture);
            Sprite outputTextureSpriteFlipped = new(firstTexture.Texture) { Scale = new Vector2f(1, -1), Position = new Vector2f(0, h) };

            Shader shader = new (null, null, "C:\\Users\\user\\_private\\_code\\client_AnotherWorld\\Another World\\OutputShader.frag");
            shader.SetUniform("u_resolution", new Vector2f(w, h));

            Random random = new();

            window.Closed += (obj, eArgs) => window.Close();
            window.MouseMoved += (obj, eArgs) =>
            {
                if (mouseHidden)
                {
                    int mx = (int)eArgs.X - w / 2;
                    int my = (int)eArgs.Y - h / 2;
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

                    dirTemp.Z = dir.Z * (float)Math.Cos(-my) - dir.X * (float)Math.Sin(-my);
                    dirTemp.X = dir.Z * (float)Math.Sin(-my) + dir.X * (float)Math.Cos(-my);
                    dirTemp.Y = dir.Y;
                    dir.X = dirTemp.X * (float)Math.Cos(mx) - dirTemp.Y * (float)Math.Sin(mx);
                    dir.Y = dirTemp.X * (float)Math.Sin(mx) + dirTemp.Y * (float)Math.Cos(mx);
                    dir.Z = dirTemp.Z;
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

                    shader.SetUniform("u_pos", pos);
                    shader.SetUniform("u_mouse", new Vector2f(mx, my));
                    shader.SetUniform("u_time", clock.ElapsedTime.AsSeconds());
                    shader.SetUniform("u_sample_part", 1.0f / framesStill);
                    shader.SetUniform("u_seed1", new Vector2f((float)random.NextDouble(), (float)random.NextDouble()) * 999.0f);
                    shader.SetUniform("u_seed2", new Vector2f((float)random.NextDouble(), (float)random.NextDouble()) * 999.0f);
                }

                if (framesStill % 2 == 1)
                {
                    shader.SetUniform("u_sample", firstTexture.Texture);
                    RenderStates states = new() { Shader = shader };
                    outputTexture.Draw(firstTextureSpriteFlipped, states);
                    window.Draw(outputTextureSprite);
                }
                else
                {
                    shader.SetUniform("u_sample", outputTexture.Texture);
                    RenderStates states = new() { Shader = shader };
                    firstTexture.Draw(outputTextureSpriteFlipped, states);
                    window.Draw(firstTextureSprite);
                }

                window.Display();
                framesStill++;
            }
            return 0;
        }
    }
}