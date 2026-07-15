package com.example.shooter.game.player;

import lombok.AllArgsConstructor;
import lombok.Getter;
import lombok.extern.slf4j.Slf4j;

@AllArgsConstructor
@Getter
@Slf4j
public enum PlayerRole {
    // Order is important here
    MEMBER,
    MODERATOR,
    CREATOR;
}
