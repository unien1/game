using Raylib_cs;
using BlackjackGame;
using System.Numerics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System;

Raylib.InitWindow(1000, 700, "Blackjack");
Raylib.SetTargetFPS(60);

Font verdanaFont;
Dictionary<string, Texture2D> cardTextures = new();
string assetsPath = Path.Combine(AppContext.BaseDirectory, "Assets");
if (!Directory.Exists(assetsPath)) assetsPath = "Assets"; 

int balance = 1000;
int bet = 100;
bool canPlay = true;

Color bgColor = new Color(5, 5, 5, 255);
Color uiPanelColor = new Color(20, 20, 25, 255);
Color accentBlue = new Color(0, 122, 255, 255);
Color hoverBlue = new Color(50, 150, 255, 255);

using (var db = new BlackjackContext())
{
  db.Database.EnsureCreated();
  var save = db.Saves.FirstOrDefault();
  if (save == null) {
    db.Saves.Add(new UserSave { Balance = 1000, LastPlayed = DateTime.Now });
    db.SaveChanges();
  } else {
    balance = save.Balance;
    if (balance < bet) canPlay = false;
  }
}

async Task SaveBalanceAsync()
{
  try 
  {
    await Task.Run(() => {
      using var db = new BlackjackContext();
      var save = db.Saves.FirstOrDefault();
      if (save != null) {
        save.Balance = balance;
        save.LastPlayed = DateTime.Now;
        db.SaveChanges();
      }
    });
  }
  catch (Exception ex)
  {
    File.AppendAllText("error.log", $"{DateTime.Now}: {ex.Message}\n");
  }
}

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
string statusMessage = "";
bool gameOver = false;

void StartGame()
{
  if (balance < bet) { canPlay = false; statusMessage = "OUT OF MONEY!"; return; }
  canPlay = true;
  deck = new(); player = new(); dealer = new();
  player.OnBust += () => { statusMessage = "YOU BUSTED!"; };
  player.Add(deck.Draw()); player.Add(deck.Draw());
  dealer.Add(deck.Draw()); dealer.Add(deck.Draw());
  gameOver = false;
  statusMessage = "BET: $" + bet;
}

void ProcessResults() 
{
  int p = player.Score();
  int d = dealer.Score();
  if (p > 21) balance -= bet;
  else if (d > 21 || p > d) { balance += bet; statusMessage = "YOU WIN! +$" + bet; }
  else if (p < d) { balance -= bet; statusMessage = "DEALER WINS! -$" + bet; }
  else statusMessage = "PUSH (DRAW)";
  if (balance < bet) canPlay = false;
  _ = SaveBalanceAsync();
}

StartGame();

void DrawTextF(string text, int x, int y, int size, Color color) => 
  Raylib.DrawTextEx(verdanaFont, text, new Vector2(x, y), (float)size, 1, color);

bool GuiButton(Rectangle rect, string text, Color baseColor)
{
  Vector2 mouse = Raylib.GetMousePosition();
  bool isHover = Raylib.CheckCollisionPointRec(mouse, rect);
  Raylib.DrawRectangleRounded(rect, 0.15f, 8, isHover ? hoverBlue : baseColor);
  Vector2 textSize = Raylib.MeasureTextEx(verdanaFont, text, 24, 1);
  DrawTextF(text, (int)(rect.x + rect.width/2 - textSize.X/2), (int)(rect.y + rect.height/2 - textSize.Y/2), 24, Color.WHITE);
  return isHover && Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT);
}

while (!Raylib.WindowShouldClose())
{
  Raylib.BeginDrawing();
  Raylib.ClearBackground(bgColor);

  Rectangle balPanel = new Rectangle(845, 30, 125, 60); 
  Raylib.DrawRectangleRounded(balPanel, 0.2f, 10, uiPanelColor);
  Raylib.DrawRectangleRoundedLines(balPanel, 0.2f, 10, 2, new Color(45, 50, 60, 255));

  DrawTextF("BALANCE", (int)balPanel.x + 15, (int)balPanel.y + 10, 14, Color.GRAY);
  DrawTextF($"$ {balance}", (int)balPanel.x + 15, (int)balPanel.y + 25, 26, Color.WHITE);

  int visScore = gameOver ? dealer.Score() : (dealer.Cards.Count > 1 ? dealer.Cards[1].GetValue() : 0);
  DrawTextF("DEALER", 220, 345, 20, Color.GRAY);
  DrawTextF(visScore.ToString(), 220, 370, 45, Color.WHITE);
  DrawHand(dealer, 220, 110, !gameOver);

  DrawTextF("YOU", 600, 155, 20, Color.GRAY);
  DrawTextF(player.Score().ToString(), 600, 180, 45, Color.WHITE);
  DrawHand(player, 600, 240, false); 

  Raylib.DrawRectangle(0, 550, 1000, 150, uiPanelColor);
  Raylib.DrawLineEx(new Vector2(0, 550), new Vector2(1000, 550), 2, new Color(40, 45, 55, 255));

  if (!gameOver && canPlay) {
    if (GuiButton(new Rectangle(250, 590, 240, 60), "HIT", accentBlue)) {
      player.Add(deck.Draw());
      if (player.Score() > 21) { gameOver = true; ProcessResults(); }
    }
    if (GuiButton(new Rectangle(510, 590, 240, 60), "STAND", accentBlue)) {
      while (dealer.Score() < 17) dealer.Add(deck.Draw());
      gameOver = true; ProcessResults();
    }
  } else {
    if (canPlay) {
      if (GuiButton(new Rectangle(350, 590, 300, 60), "NEW DEAL", accentBlue)) StartGame();
    } else {
      if (GuiButton(new Rectangle(350, 590, 300, 60), "RELOAD ($1000)", Color.ORANGE)) {
        balance = 1000;
        _ = SaveBalanceAsync();
        StartGame();
      }
    }
    Vector2 msgSize = Raylib.MeasureTextEx(verdanaFont, statusMessage, 26, 1);
    DrawTextF(statusMessage, (int)(500 - msgSize.X/2), 555, 26, new Color(255, 204, 0, 255));
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
    Color borderCol = new Color(50, 55, 65, 255); 
    for (int i = 0; i < hand.Cards.Count; i++) {
        string n = (i == 0 && hide) ? "back" : GetCardFileName(hand.Cards[i].Suit, hand.Cards[i].Rank);
        if (cardTextures.ContainsKey(n)) {
            Texture2D tex = cardTextures[n];
            Rectangle cardRect = new Rectangle(x + (i * spacing) - 2, y - 2, (tex.width * scale) + 4, (tex.height * scale) + 4);
            Raylib.DrawRectangleRounded(cardRect, 0.1f, 10, Color.WHITE);
            Raylib.DrawRectangleRoundedLines(cardRect, 0.1f, 10, 2, borderCol);
            Raylib.DrawTextureEx(tex, new Vector2(x + (i * spacing), y), 0, scale, Color.WHITE);
        }
    }
}