﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Polynomials
{
    public class Polynomial
    {
        private readonly LinkedList<Monomial> _content;

        public Polynomial()
        {
            _content = new LinkedList<Monomial>();
        }

        private Polynomial(IEnumerable<Monomial> monomials)
        {
            _content = new LinkedList<Monomial>(monomials.OrderByDescending(m => m.Power));
            GroupUp();
            Purify();
        }

        public Polynomial(IEnumerable<(double, uint)> monomials)
            : this(monomials.Select(m => new Monomial(m.Item1, m.Item2)))
        { }

        public Polynomial(params (double, uint)[] monomials) : this(monomials.AsEnumerable())
        { }

        public uint Degree => _content.First?.Value.Power ?? 0;

        public int Count => _content.Count;

        public bool IsZero => _content.All(m => m.Multiplier == 0);

        public double this[uint power]
        {
            set
            {
                if (TryGetByPower(power, out LinkedListNode<Monomial> node))
                {
                    if(value != 0)
                        node.Value = node.Value with { Multiplier = value };
                    else
                        _content.Remove(node);
                } 
                else if (value != 0)
                {
                    if (node == null)
                        _content.AddFirst(new Monomial(value, power));
                    else
                        _content.AddBefore(node, new Monomial(value, power));
                }
            }
        }

        private bool TryGetByPower(uint power, out LinkedListNode<Monomial> node)
        {
            node = _content.First;
            if (node == null)
                return false;

            while (node.Next != null && node.Next.Value.Power > power)
                node = node.Next;
            return node != null && node.Value.Power == power;
        }

        public Polynomial Add(Polynomial another)
        {
            return Merge(another);
        }

        public static Polynomial operator +(Polynomial lhs, Polynomial rhs)
        {
            return lhs.Add(rhs);
        }

        public Polynomial Subtract(Polynomial another)
        {
            return Add(another.Multiply(-1f));
        }
        
        public static Polynomial operator -(Polynomial lhs, Polynomial rhs)
        {
            return lhs.Subtract(rhs);
        }

        public Polynomial Multiply(double multiplier)
        {
            return new Polynomial(
                _content.Select(m => m with { Multiplier = m.Multiplier * multiplier }));
        }
        
        public static Polynomial operator *(Polynomial lhs, double rhs)
        {
            return lhs.Multiply(rhs);
        }

        public Polynomial Multiply(Monomial multiplier)
        {
            return new Polynomial(_content.Select(m => new Monomial(
                                                      m.Multiplier * multiplier.Multiplier,
                                                      m.Power + multiplier.Power)));
        }
        
        public static Polynomial operator *(Polynomial lhs, Monomial rhs)
        {
            return lhs.Multiply(rhs);
        }

        public Polynomial Multiply(Polynomial multiplier)
        {
            if (IsZero || multiplier.IsZero)
                return new Polynomial();
            if (Count == 1)
                return multiplier.Multiply(_content.First.Value);
            if (multiplier.Count == 1)
                return Multiply(multiplier._content.First.Value);

            return new Polynomial(_content.SelectMany(m => multiplier.Multiply(m)._content));
        }
        
        public static Polynomial operator *(Polynomial lhs, Polynomial rhs)
        {
            return lhs.Multiply(rhs);
        }

        public Polynomial Divide(double divisor)
        {
            return Multiply(1f / divisor);
        }

        public static implicit operator Polynomial(double x)
        {
            return new Polynomial((x, 0));
        }
        
        private Polynomial Merge(Polynomial another)
        {
            // there could be some cool merging logic, I know
            return new Polynomial(_content.Concat(another._content));
        }

        private void GroupUp()
        {
            if (Count < 1)
                return;

            LinkedListNode<Monomial> node = _content.First;
            while (node != null)
            {
                if (node.Next != null && node.Value.Power == node.Next.Value.Power)
                {
                    double sum = node.Value.Multiplier + node.Next.Value.Multiplier;
                    node.Value = node.Value with { Multiplier = sum };
                    _content.Remove(node.Next);
                }
                else
                {
                    node = node.Next;
                }
            }
        }

        private void Purify()
        {
            if (Count == 0)
                return;

            LinkedListNode<Monomial> node = _content.First;
            while (node != null)
            {
                if (node.Value.Multiplier == 0f)
                {
                    LinkedListNode<Monomial> next = node.Next;
                    _content.Remove(node);
                    node = next;
                }
                else
                {
                    node = node.Next;
                }
            }
        }

        public override string ToString()
        {
            switch (Count)
            {
            case 0:
                return "0";
            case 1:
                return _content.First.Value.ToString();
            default:
            {
                var tail =
                    string.Join(' ',
                                _content.Skip(1)
                                        .Select(m =>
                                                    $"{(m.Multiplier > 0 ? '+' : '-')} {m.ToStringUnsigned()}"));

                return $"{_content.First.Value} {tail}";
            }
            }
        }

        public static Polynomial Parse(string pattern)
        {
            // ReSharper disable once IdentifierTypo
            var monoms = new LinkedList<Monomial>();

            pattern = pattern.EraseWhitespaces();
            if (pattern[0] != '-' && pattern[0] != '+')
                pattern = $"+{pattern}";

            var signs = new LinkedList<int>();
            var i = 0;
            for (; i < pattern.Length; ++i)
            {
                if (pattern[i] == '+' || pattern[i] == '-')
                    signs.AddLast(i);
            }

            foreach (int sign in signs)
            {
                var j = 0;
                uint power = 0;
                double multiplier = pattern[sign] == '-' ? -1f : 1f;
                i = sign + 1;
                while (i+j < pattern.Length && (char.IsDigit(pattern[i+j]) || pattern[i+j] == '.'))
                    ++j;
                if (j > 0)
                {
                    string token = pattern[i..(i + j)];
                    if (!double.TryParse(token, out double m))
                        throw new FormatException(
                            $"Invalid numeric token at {i}:{i + j}: '{token}'");
                    multiplier *= m;
                }

                i += j;
                if (i < pattern.Length && pattern[i] == 'x')
                {
                    power = 1;
                    ++i;
                    if (i < pattern.Length && pattern[i] == '^')
                    {
                        j = 0;
                        ++i;
                        while (i+j < pattern.Length && char.IsDigit(pattern[i + j]))
                            ++j;
                        if (j > 0)
                        {
                            string token = pattern[i..(i + j)];
                            if (!int.TryParse(token, out int m))
                                throw new FormatException(
                                    $"Invalid numeric token at {i}:{i + j}: '{token}'");
                            power = (uint)m;
                        }
                        else
                        {
                            throw new FormatException($"No integer after '^' at {i}");
                        }
                    }
                }

                monoms.AddLast(new Monomial(multiplier, power));
            }

            return new Polynomial(monoms);
        }
    }
}
