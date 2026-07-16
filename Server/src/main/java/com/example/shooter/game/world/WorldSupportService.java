package com.example.shooter.game.world;

import com.example.shooter.game.player.Player;
import com.example.shooter.game.player.PlayerRepository;
import com.example.shooter.game.player.PlayerRole;
import lombok.extern.slf4j.Slf4j;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;

import java.util.Comparator;
import java.util.List;
import java.util.UUID;
import java.util.concurrent.TimeUnit;

@Service
@Slf4j
public class WorldSupportService {

    private final WorldRepository worldRepository;
    private final PlayerRepository playerRepository;

    public WorldSupportService(WorldRepository worldRepository, PlayerRepository playerRepository) {
        this.worldRepository = worldRepository;
        this.playerRepository = playerRepository;
    }

    @Transactional
    public void fix(UUID worldId) {
        World world = worldRepository.findByIdForPessimisticWrite(worldId).orElse(null);
        if (world == null) return;

        List<Player> players = playerRepository.findAllByWorldId(worldId);

        if (players.isEmpty()) {
            worldRepository.delete(world);
            log.info("fix: deleted abandoned world {}", worldId);
            return;
        }

        if (players.stream().anyMatch(p -> p.getRole() == PlayerRole.CREATOR)) {
            log.info("fix: world {} no action required", worldId);
        }

        Player successor = players.stream()
                .min(Comparator.comparing((Player p) -> p.getRole().ordinal()).reversed()
                        .thenComparing(Player::getMemberSince)
                        .thenComparing(Player::getId))
                .orElseThrow();
        successor.setRole(PlayerRole.CREATOR);
        playerRepository.save(successor);
        log.info("fix: world {} creator passed to user {}", worldId, successor.getUser().getId());
    }

    @Scheduled(fixedDelay = 8, timeUnit = TimeUnit.HOURS)
    @Transactional
    public void fixAll() {
        log.info("fixing worlds...");
        List<UUID> worldIds = worldRepository.findWorldIdsWithoutRole(PlayerRole.CREATOR);
        for (UUID worldId : worldIds) {
            fix(worldId);
        }
        if (!worldIds.isEmpty()) {
            log.info("fixAll: processed {} worlds", worldIds.size());
        }
    }
}
