package com.example.shooter.game.world.unity;

import com.fasterxml.jackson.annotation.JsonInclude;
import lombok.AllArgsConstructor;
import lombok.Getter;

import java.util.UUID;

@Getter
@AllArgsConstructor
@JsonInclude(JsonInclude.Include.NON_NULL)
public class UnityHook {
    private final UnityHookAction action;
    private final Long userId;
    private final UUID worldId;
}
