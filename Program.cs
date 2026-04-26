using Raylib_cs;
using BlackjackGame;
using System.Numerics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;

Raylib.InitWindow(1000, 700, "Blackjack Project");
Raylib.SetTargetFPS(60);

Font verdanaFont;
Dictionary<string, Texture2D> cardTextures = new();
string assetsPath = Path.Combine(AppContext.BaseDirectory, "Assets");
if (!Directory.Exists(assetsPath)) assetsPath = "Assets"; 

int balance = 1000;
int bet = 100;
bool canPlay = true;

void LoadResources()
{
  string fontPath = Path.Combine(assetsPath, "verdana-bold.ttf");
  verdanaFont = File.Exists(fontPath) ? Raylib.LoadFontEx(fontPath, 64, null, 0) : Raylib.GetFontDefault();
  
  foreach (Suit s in Enum.GetValues(typeof(Suit)))
    foreach (Rank r in Enum.GetValues(typeof(Rank))) {
      string name = GetCardFileName(s, r);
      string path = Path.Combine(assetsPath, $"{name}.png");
      if (File.Exists(path)) cardTextures[name] = Raylib.LoadTexture(path);
    }
  cardTextures["back"] = Raylib.LoadTexture(Path.Combine(assetsPath, "back.png"));
}

LoadResources();

Deck deck = new();
Hand player = new();
Hand dealer = new();
string statusMessage = "WELCOME";
bool gameOver = false;

void StartGame()
{
  if (balance < bet) { canPlay = false; return; }
  canPlay = true;
  deck = new(); player = new(); dealer = new();
  player.Add(deck.Draw()); player.Add(deck.Draw());
  dealer.Add(deck.Draw()); dealer.Add(deck.Draw());
  gameOver = false;
}

void ProcessResults() 
{
  int p = player.Score();
  int d = dealer.Score();
  if (p > 21) balance -= bet;
  else if (d > 21 || p > d) { balance += bet; statusMessage = "WIN"; }
  else if (p < d) { balance -= bet; statusMessage = "LOSE"; }
  else statusMessage = "PUSH";
}

void DrawTextF(string text, int x, int y, int size, Color color) => 
  Raylib.DrawTextEx(verdanaFont, text, new Vector2(x, y), (float)size, 1, color);

bool GuiButton(Rectangle rect, string text, Color baseColor)
{
  Vector2 mouse = Raylib.GetMousePosition();
  bool isHover = Raylib.CheckCollisionPointRec(mouse, rect);
  Raylib.DrawRectangleRounded(rect, 0.15f, 8, isHover ? Color.GRAY : baseColor);
  Vector2 textSize = Raylib.MeasureTextEx(verdanaFont, text, 24, 1);
  DrawTextF(text, (int)(rect.x + rect.width/2 - textSize.X/2), (int)(rect.y + rect.height/2 - textSize.Y/2), 24, Color.WHITE);
  return isHover && Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT);
}

while (!Raylib.WindowShouldClose())
{
  Raylib.BeginDrawing();
  Raylib.ClearBackground(new Color(30, 35, 45, 255));

  DrawTextF($"BALANCE: ${balance}", 750, 40, 20, Color.WHITE);

  int visScore = gameOver ? dealer.Score() : (dealer.Cards.Count > 1 ? dealer.Cards[1].GetValue() : 0);
  DrawTextF("DEALER", 220, 345, 20, Color.GRAY);
  DrawTextF(visScore.ToString(), 220, 370, 45, Color.WHITE);
  DrawHand(dealer, 220, 110, !gameOver);

  DrawTextF("YOU", 600, 155, 20, Color.GRAY);
  DrawTextF(player.Score().ToString(), 600, 180, 45, Color.WHITE);
  DrawHand(player, 600, 240, false); 

  Raylib.DrawRectangle(0, 550, 1000, 150, new Color(40, 45, 55, 255));

  if (!gameOver && canPlay) {
    if (GuiButton(new Rectangle(250, 590, 240, 60), "HIT", Color.DARKGRAY)) {
      player.Add(deck.Draw());
      if (player.Score() > 21) { gameOver = true; ProcessResults(); }
    }
    if (GuiButton(new Rectangle(510, 590, 240, 60), "STAND", Color.DARKGRAY)) {
      while (dealer.Score() < 17) dealer.Add(deck.Draw());
      gameOver = true; ProcessResults();
    }
  } else {
    if (GuiButton(new Rectangle(350, 590, 300, 60), "START", Color.DARKGRAY)) StartGame();
  }
  Raylib.EndDrawing();
}

foreach (var t in cardTextures.Values) Raylib.UnloadTexture(t);
Raylib.UnloadFont(verdanaFont);
Raylib.CloseWindow();

string GetCardFileName(Suit suit, Rank rank)
{
  string s = suit.ToString().ToLower();
  string r = (int)rank <= 10 ? ((int)rank).ToString() : rank.ToString().ToLower();
  return $"{r}_of_{s}";
}

void DrawHand(Hand hand, int x, int y, bool hide) {
    float scale = 0.65f; 
    int spacing = 60; 
    for (int i = 0; i < hand.Cards.Count; i++) {
        string n = (i == 0 && hide) ? "back" : GetCardFileName(hand.Cards[i].Suit, hand.Cards[i].Rank);
        if (cardTextures.ContainsKey(n)) {
            Texture2D tex = cardTextures[n];
            Raylib.DrawRectangleRounded(new Rectangle(x+(i*spacing)-2, y-2, (tex.width*scale)+4, (tex.height*scale)+4), 0.1f, 10, Color.WHITE);
            Raylib.DrawTextureEx(tex, new Vector2(x+(i*spacing), y), 0, scale, Color.WHITE);
        }
    }
}