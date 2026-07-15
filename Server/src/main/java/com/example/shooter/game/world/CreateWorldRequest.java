package com.example.shooter.game.world;

import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Size;
import lombok.Getter;
import lombok.Setter;

@Getter
@Setter
public class CreateWorldRequest {
    @NotNull
    @Size(min = WorldConstants.MIN_NAME_LENGTH, max = WorldConstants.MAX_NAME_LENGTH)
    private String name;

    @NotNull
    @Size(min = WorldConstants.MIN_DESCRIPTION_LENGTH, max = WorldConstants.MAX_DESCRIPTION_LENGTH)
    private String description;

    @NotNull
    private WorldVisibilityPolicy visibilityPolicy;

    @NotNull
    private WorldJoinPolicy joinPolicy;
}
