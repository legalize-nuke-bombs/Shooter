package com.example.shooter.game.world;

import com.example.shooter.game.player.PlayerRole;
import jakarta.validation.constraints.Max;
import jakarta.validation.constraints.Min;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.security.core.annotation.AuthenticationPrincipal;
import org.springframework.validation.annotation.Validated;
import org.springframework.web.bind.annotation.*;

import java.util.List;
import java.util.UUID;

@RestController
@RequestMapping("/api/worlds")
@Validated
public class WorldController {

    private final WorldService worldService;
    private final WorldMetricService worldMetricService;

    public WorldController(WorldService worldService, WorldMetricService worldMetricService) {
        this.worldService = worldService;
        this.worldMetricService = worldMetricService;
    }

    @PostMapping
    public ResponseEntity<WorldRepresentation> create(@AuthenticationPrincipal Long userId, @RequestBody @Validated CreateWorldRequest request) {
        return ResponseEntity.status(HttpStatus.CREATED).body(
                worldService.create(userId, request)
        );
    }

    @PostMapping("/{worldId}/join")
    public WorldJoinResponse join(@AuthenticationPrincipal Long userId, @PathVariable UUID worldId) {
        return worldService.join(userId, worldId);
    }

    @GetMapping
    public List<WorldRepresentation> get(
            @AuthenticationPrincipal Long userId,
            @RequestParam(required = false) PlayerRole playerRole,
            @RequestParam(defaultValue = "0") @Min(0) Integer page,
            @RequestParam(defaultValue = "100") @Min(0) @Max(100) Integer size
            ) {
        return worldService.get(userId, playerRole, page, size);
    }

    @GetMapping("/metrics")
    public WorldMetricRepresentation getMetrics() {
        return worldMetricService.get();
    }
}
