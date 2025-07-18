﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biblioteka
{
    public static class PocetniApartmani
    {
        public static List<Apartman> KreirajPocetneApartmane()
        {
            List<Apartman> apartmani = new List<Apartman>();
            Apartman a1 = new Apartman {
                BrojAp = 1,
                Klasa = Klasa.I,
                MaxBrGostiju = 4,
                TrenutniBrGostiju = 0,
                Stanje = Stanje.PRAZAN,
                Alarm = Alarm.NORMALNO,
                Gosti = new List<Gost>(),
                BrojNocenja = 0,
                Troskovi = 400,
                RacunCeo = "-------------------\n"
            };
            apartmani.Add(a1);

            Apartman a2 = new Apartman
            {
                BrojAp = 2,
                Klasa = Klasa.I,
                MaxBrGostiju = 2,
                TrenutniBrGostiju = 0,
                Stanje = Stanje.PRAZAN,
                Alarm = Alarm.NORMALNO,
                Gosti = new List<Gost>(),
                BrojNocenja = 0,
                Troskovi = 350,
                RacunCeo = "-------------------\n"
            };
            apartmani.Add(a2);

            Apartman a3 = new Apartman
            {
                BrojAp = 3,
                Klasa = Klasa.II,
                MaxBrGostiju = 3,
                TrenutniBrGostiju = 0,
                Stanje = Stanje.PRAZAN,
                Alarm = Alarm.NORMALNO,
                Gosti = new List<Gost>(),
                BrojNocenja = 0,
                Troskovi = 300,
                RacunCeo = "-------------------\n"
            };
            apartmani.Add(a3);

            Apartman a4 = new Apartman
            {
                BrojAp = 4,
                Klasa = Klasa.II,
                MaxBrGostiju = 2,
                TrenutniBrGostiju = 0,
                Stanje = Stanje.PRAZAN,
                Alarm = Alarm.NORMALNO,
                Gosti = new List<Gost>(),
                BrojNocenja = 0,
                Troskovi = 350,
                RacunCeo = "-------------------\n"
            };
            apartmani.Add(a4);

            Apartman a5 = new Apartman
            {
                BrojAp = 5,
                Klasa = Klasa.III,
                MaxBrGostiju = 2,
                TrenutniBrGostiju = 0,
                Stanje = Stanje.PRAZAN,
                Alarm = Alarm.NORMALNO,
                Gosti = new List<Gost>(),
                BrojNocenja = 0,
                Troskovi = 200,
                RacunCeo = "-------------------\n"
            };
            apartmani.Add(a5);

            return apartmani;
        }
    }
}
