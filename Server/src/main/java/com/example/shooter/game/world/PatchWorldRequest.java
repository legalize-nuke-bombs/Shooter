package com.example.shooter.game.world;

import jakarta.validation.constraints.Size;
import lombok.Getter;
import lombok.Setter;

@Getter
@Setter
public class PatchWorldRequest {
    @Size(min = WorldConstants.MIN_NAME_LENGTH, max = WorldConstants.MAX_NAME_LENGTH)
    private String name;

    private WorldJoinPolicy joinPolicy;
}
