using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace BlackjackGame
{
    public enum Suit { Hearts, Diamonds, Clubs, Spades }
    public enum Rank { Two = 2, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King, Ace }

    public class UserSave
    {
        public int Id { get; set; }
        public int Balance { get; set; }
        public DateTime LastPlayed { get; set; }
    }

    public class BlackjackContext : DbContext
    {
        public DbSet<UserSave> Saves { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite("Data Source=blackjack.db");
    }

    public class Card
    {
        public Suit Suit { get; }
        public Rank Rank { get; }
        public Card(Suit suit, Rank rank) { Suit = suit; Rank = rank; }
        public int GetValue() => (int)Rank <= 10 ? (int)Rank : (Rank == Rank.Ace ? 11 : 10);
    }

    public class Deck
    {
        private List<Card> cards = new();
        private Random rng = new();
        public Deck()
        {
            foreach (Suit s in Enum.GetValues(typeof(Suit)))
                foreach (Rank r in Enum.GetValues(typeof(Rank)))
                    cards.Add(new Card(s, r));
            cards = cards.OrderBy(x => rng.Next()).ToList();
        }
        public Card Draw() => cards.Count > 0 ? cards.PopAt(0) : new Card(Suit.Hearts, Rank.Ace);
    }

    public class Hand
    {
        public event Action? OnBust; 

        public List<Card> Cards { get; } = new();
        public void Add(Card c) 
        { 
            Cards.Add(c);
            if (Score() > 21) OnBust?.Invoke(); 
        }

        public int Score()
        {
            int val = Cards.Sum(c => c.GetValue());
            int aces = Cards.Count(c => c.Rank == Rank.Ace);
            while (val > 21 && aces > 0) { val -= 10; aces--; }
            return val;
        }
    }

    public static class ListExtensions 
    {
        public static T PopAt<T>(this List<T> list, int index)
        {
            T r = list[index];
            list.RemoveAt(index);
            return r;
        }
    }
}