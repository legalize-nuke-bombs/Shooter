package com.example.shooter.server;

import lombok.AllArgsConstructor;
import lombok.Getter;

@Getter
@AllArgsConstructor
public class ServerRepresentation {
    private final String name;
    private final int major;
    private final int minor;
    private final int patch;
}
