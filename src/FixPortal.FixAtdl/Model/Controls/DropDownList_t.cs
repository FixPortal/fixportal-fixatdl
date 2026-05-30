// FP Enhancement: 2026-05-24 — modernised for net10 (file-scoped, nullable, FixPortal namespace).
#region Copyright (c) 2010-2011, Steve Wilkinson (author)
// This software is released under the MIT License..
// If you received this software under the commercial license, the terms of that license can be found in the
// Commercial.txt file in the Licenses folder.  If you received this software under the open-source license,
// the following applies:
//
//   This file is part of Atdl4net.
//
//   Atdl4net is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public 
//   License as published by the Free Software Foundation, version 3.
// 
//   Atdl4net is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
//   of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public License for more details.
//
//   You should have received a copy of the GNU Lesser General Public License along with Atdl4net.  If not, see
//   http://www.gnu.org/licenses/.
#endregion

using FixPortal.FixAtdl.Model.Controls.Support;

namespace FixPortal.FixAtdl.Model.Controls;

/// <summary>
/// Represents the DropDownList_t control element within FIXatdl.
/// </summary>
public class DropDownList_t : ListControlBase
{
    /// <summary>
    /// Initializes a new instance of <see cref="DropDownList_t"/> using the supplied ID.
    /// </summary>
    /// <param name="id">ID for this control.</param>
    public DropDownList_t(string id)
        : base(id)
    {
    }
}
