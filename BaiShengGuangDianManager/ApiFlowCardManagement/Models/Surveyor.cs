﻿using System;
using System.Collections.Generic;

namespace ApiFlowCardManagement.Models
{
    public class Surveyor
    {
        public int Id { get; set; }
        public string CreateUserId { get; set; }
        public DateTime MarkedDateTime { get; set; }
        public bool MarkedDelete { get; set; }
        public int ModifyId { get; set; }
        public string SurveyorName { get; set; }

    }
}
