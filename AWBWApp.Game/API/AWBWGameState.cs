using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AWBWApp.Game.API
{
    public class AWBWGameState
    {
        [JsonProperty(ItemConverterType = typeof(ArrayOrObjectConverterToIntDictionary<AWBWTile>))]
        public Dictionary<int, Dictionary<int, AWBWTile>> Terrain;
        [JsonProperty(ItemConverterType = typeof(ArrayOrObjectConverterToIntDictionary<AWBWBuilding>))]
        public Dictionary<int, Dictionary<int, AWBWBuilding>> Buildings;

        [JsonProperty]
        public Dictionary<long, AWBWUnit> Units;

        [JsonProperty]
        public long CurrentTurnPId;

        [JsonProperty("gameTurn")]
        public int CurrentTurnIdx;

        [JsonProperty]
        public AWBWWeather GameWeather;

        [JsonProperty]
        public Dictionary<long, AWBWGamePlayer> Players;
    }

    public class AWBWGamePlayer
    {
        [JsonProperty("players_id")]
        public long PlayerID;

        [JsonProperty("users_username")]
        public string Username;

        #region GameInformation

        [JsonProperty("players_countries_id")]
        public int CountryId;

        [JsonProperty("players_co_id")]
        public int COId;

        #endregion

        public bool IsEliminated => eliminated == "Y" || eliminated == "y";

        [JsonProperty("players_eliminated")]
        private string eliminated;

        [JsonProperty("players_team")]
        public string TeamId; //What is this used for

        [JsonProperty("players_order")]
        public int TurnPriority;
    }

    public class AWBWBuilding
    {
        [JsonProperty("buildings_x")]
        public int X { get; set; }

        [JsonProperty("buildings_y")]
        public int Y { get; set; }

        [JsonProperty("buildings_id")]
        public long ID { get; set; }

        [JsonProperty("buildings_players_id")]
        public long? CapturedBy;

        [JsonProperty("buildings_capture")]
        public long BuildingHP;

        [JsonProperty]
        public int Terrain_Id { get; set; }
    }

    public class AWBWWeather
    {
        [JsonProperty]
        public string Code { get; set; }
    }

    public class AWBWTile
    {
        [JsonProperty]
        public int Tiles_X { get; set; }

        [JsonProperty]
        public int Tiles_Y { get; set; }

        [JsonProperty]
        public int Terrain_Id { get; set; }

        [JsonProperty]
        public string Terrain_Name { get; set; }

        [JsonProperty]
        public string Terrain_Country_Code { get; set; }
    }

    public class AWBWUnit
    {
        [JsonProperty("units_x")]
        public int X { get; set; }

        [JsonProperty("units_y")]
        public int Y { get; set; }

        [JsonProperty("units_id")]
        public long ID { get; set; }

        [JsonProperty("units_players_id")]
        public long? OwnedBy;

        [JsonProperty("units_fuel")]
        public int Fuel;

        [JsonProperty("units_ammo")]
        public int Ammo;

        [JsonProperty("units_hit_points")]
        public int HitPoints;

        [JsonProperty("units_moved")]
        public bool HasMoved;

        [JsonProperty("units_capture")]
        public bool HasCaptured;

        [JsonProperty("units_fired")]
        public bool HasFired;

        [JsonProperty("units_cargo1_units_id")]
        public string Cargo1;

        [JsonProperty("units_cargo2_units_id")]
        public string Cargo2;

        [JsonProperty("units_sub_dive")]
        public string UnitDived;

        [JsonProperty("generic_id")]
        public int? UnitId { get; set; }

        [JsonProperty("units_name")]
        public string UnitCode { get; set; }

        [JsonProperty("countries_code")]
        public string CountryCode { get; set; }
    }

    public class AWBWAttackCOP
    {
        [JsonProperty]
        public AWBWAttackCOPValue Attacker;
        [JsonProperty]
        public AWBWAttackCOPValue Defender;
    }

    public class AWBWAttackCOPValue
    {
        [JsonProperty]
        public long PlayerId;
        [JsonProperty]
        public long? COPValue;
        [JsonProperty]
        public long? TagValue;
    }

    public class ArrayOrObjectConverterToIntDictionary<T> : JsonConverter
    {
        //Todo: Make this a more generic list decoder

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var jToken = JToken.Load(reader);

            switch (jToken.Type)
            {
                case JTokenType.Object:
                {
                    return jToken.ToObject(objectType, serializer);
                }

                case JTokenType.Array:
                {
                    var list = serializer.Deserialize<IList<T>>(jToken.CreateReader());
                    var result = new Dictionary<int, T>();
                    var idx = 0;

                    foreach (var entry in list)
                    {
                        result.Add(idx, entry);
                        idx++;
                    }
                    return result;
                }

                default:
                    throw new JsonSerializationException();
            }
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Dictionary<int, object>);
        }
    }
}
