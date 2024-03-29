﻿using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System;
namespace GComFuelManager.Shared.Modelos
{
	public class Estado
	{
        [JsonProperty("cod"), Key]
        public byte Cod { get; set; } 

        [JsonProperty("den"), MaxLength(128)]
        public string? den { get; set; } = string.Empty;
    }
}

