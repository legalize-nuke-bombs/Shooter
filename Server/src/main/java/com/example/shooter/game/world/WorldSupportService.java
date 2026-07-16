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

        log.info("fixing {}...", worldId);

        List<Player> players = playerRepository.findAllByWorldId(worldId);

        long oldPlayersNumber = world.getPlayers();
        long playersNumber = players.size();

        if (oldPlayersNumber != playersNumber) {
            world.setPlayers(playersNumber);
            worldRepository.save(world);
            log.info("fix {}: number of players {} => {}", worldId, oldPlayersNumber, playersNumber);
        }

        if (playersNumber == 0) {
            worldRepository.delete(world);
            log.info("fix {}: deleted because it was abandoned", worldId);
        }

        if (playersNumber != 0 && players.stream().noneMatch(p -> p.getRole() == PlayerRole.CREATOR)) {
            Player successor = players.stream()
                    .min(Comparator.comparing((Player p) -> p.getRole().ordinal()).reversed()
                            .thenComparing(Player::getMemberSince)
                            .thenComparing(Player::getId))
                    .orElseThrow();
            successor.setRole(PlayerRole.CREATOR);
            playerRepository.save(successor);
            log.info("fix {}: creator passed to user {}", worldId, successor.getUser().getId());
        }
    }

    @Scheduled(fixedDelay = 8, timeUnit = TimeUnit.HOURS)
    @Transactional
    public void fixAll() {
        // Really inefficient but it's ok for fixedDelay 8h
        log.info("fixing worlds...");
        List<UUID> worldIds = worldRepository.findAllIds();
        for (UUID worldId : worldIds) {
            fix(worldId);
        }
        log.info("fixing: processed {} worlds", worldIds.size());
    }
}
