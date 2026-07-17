package com.example.shooter.game.world.metric;

import lombok.AllArgsConstructor;
import lombok.Getter;

@Getter
@AllArgsConstructor
public class WorldMetricRepresentation {
    private final Long totalWorlds;
    private final Long totalWorlds24h;
    private final Long activeWorlds;
}
